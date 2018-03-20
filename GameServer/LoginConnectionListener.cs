using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace GameServer
{
    public static class LoginServerInterface
    {
        internal static Connection LoginServerConnection = null;
    }

    class LoginConnectionListener
    {
        int bufBlockSize;
        byte[] recvBuffer;
        byte[] sendBuffer;
        Socket listenSocket;
        SocketListenerSettings socketListenerSettings;
        Connection loginServerConnection;
        Semaphore theMaxConnectionsEnforcer;
        AccesPermisionsLoginServer accesPermisions;

        public LoginConnectionListener(int bufsize, SocketListenerSettings theSocketListenerSettings)
        {
            bufBlockSize = bufsize;
            this.socketListenerSettings = theSocketListenerSettings;
        }

        public bool Init()
        {
            theMaxConnectionsEnforcer = new Semaphore(socketListenerSettings.MaxConnections, socketListenerSettings.MaxConnections);
            recvBuffer = new byte[bufBlockSize];
            sendBuffer = new byte[bufBlockSize];
            loginServerConnection = CreateNewConnection();
            accesPermisions = new AccesPermisionsLoginServer();
            Output.WriteLine("LoginConnectionListener::Init done");
            return true;
        }

        internal Connection CreateNewConnection()
        {
            Connection con = new Connection();
            con.client.PrivateKey = Program.loginKey;
            //accept - SocketAsyncEventArgs.Completed is an event, (the only event) 
            con.AcceptSocket.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            //set self as reference in UserToken
            con.AcceptSocket.UserToken = con;
            //objects that do receive/send operations need a buffer, assign a byte buffer from the buffer block to this particular SocketAsyncEventArg object
            con.RecvSocket.SetBuffer(recvBuffer, 0, bufBlockSize);
            con.RecvSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //for send
            con.SendSocket.SetBuffer(sendBuffer, 0, bufBlockSize);
            con.SendSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            return con;
        }

        public void StartListing()
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(this.socketListenerSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //bind it to the port
            listenSocket.Bind(this.socketListenerSettings.LocalEndPoint);
            // Start the listener with a backlog of however many connections.
            //"backlog" means pending connections. 
            //The backlog number is the number of clients that can wait for a
            //SocketAsyncEventArg object that will do an accept operation.
            //The listening socket keeps the backlog as a queue. The backlog allows 
            //for a certain # of excess clients waiting to be connected.
            //If the backlog is maxed out, then the client will receive an error when
            //trying to connect. Max # for backlog can be limited by the operating system.
            listenSocket.Listen(this.socketListenerSettings.Backlog);
            Output.WriteLine(ConsoleColor.DarkGreen, "********************* Game Server is listening for Login Server connection *************************");
            // Calls the method which will post accepts on the listening socket.            
            StartAccept();
        }

        // Begins an operation to accept a connection request from the client         
        internal void StartAccept()
        {
            bool willRaiseEvent = false;
            //wait for free accept object in pool
            this.theMaxConnectionsEnforcer.WaitOne();
            //Get a SocketAsyncEventArgs object to accept the connection.                        
            if (loginServerConnection.AcceptSocket.AcceptSocket != null)
            {
                Output.WriteLine(ConsoleColor.Red, "LoginConnectionListener::StartAccept&0001 " + " AcceptSocket still in use LOGIN SERVER CONNECTION WONT BE ESTABLISHED!");
                return;
            }
            //set individual ID for this connections (for live time of this connection)
            loginServerConnection.AssignTokenId();
            Output.WriteLine("LoginConnectionListener::StartAccept " + "New accept connection started");
            if (loginServerConnection == null)
            {
                throw new ArgumentNullException("LoginConnectionListener::StartAccept&0002 " + "Connection can't be NULL!");
            }
            //Socket.AcceptAsync begins asynchronous operation to accept the connection.
            try
            {
                willRaiseEvent = listenSocket.AcceptAsync(loginServerConnection.AcceptSocket);
            }
            catch (ObjectDisposedException e)
            {
                Output.WriteLine("LoginConnectionListener::StartAccept&0003 Async listener: object disposed error");
                return;
            }
            //Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is 
            //working asynchronously. The SocketAsyncEventArgs.Completed event on the acceptEventArg parameter 
            //will be raised upon completion of accept op.
            //AcceptAsync will call the AcceptEventArg_Completed
            //method when it completes, because when we created this SocketAsyncEventArgs
            //object before putting it in the pool, we set the event handler to do it.
            //AcceptAsync returns false if the I/O operation completed synchronously.            
            //The SocketAsyncEventArgs.Completed event on the acceptEventArg 
            //parameter will NOT be raised when AcceptAsync returns false.
            if (!willRaiseEvent)
            {
                //The code in this if (!willRaiseEvent) statement only runs 
                //when the operation was completed synchronously. It is needed because 
                //when Socket.AcceptAsync returns false, 
                //it does NOT raise the SocketAsyncEventArgs.Completed event.
                //And we need to call ProcessAccept and pass it the SAEA object.
                //This is only when a new connection is being accepted.
                // Probably only relevant in the case of a socket error.
                ProcessAccept(loginServerConnection.AcceptSocket);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            bool canConnect = false;
            //relase accept object
            //this.theMaxAcceptConnectionsEnforcer.Release();
            // This is when there was an error with the accept op. That should NOT
            // be happening often. It could indicate that there is a problem with
            // that socket. If there is a problem, then we would have an infinite
            // loop here, if we tried to reuse that same socket.
            if (acceptEventArgs.SocketError != SocketError.Success)
            {
                Output.WriteLine(ConsoleColor.Red, "LoginConnectionListener::ProcessAccept SocketError, accept from: " + acceptEventArgs.RemoteEndPoint.ToString());
                //Let's destroy this socket, since it could be bad.
                HandleBadAccept((Connection)acceptEventArgs.UserToken);
                // Loop back to post another accept op. Notice that we are NOT
                StartAccept();
                return;
            }
            if (acceptEventArgs.AcceptSocket != null && (System.Net.IPEndPoint)acceptEventArgs.AcceptSocket.RemoteEndPoint != null)
            {
                canConnect = accesPermisions.CanConnect((System.Net.IPEndPoint)acceptEventArgs.AcceptSocket.RemoteEndPoint);
            }
            if (!canConnect)
            {
                HandleBadAccept((Connection)acceptEventArgs.UserToken);
                StartAccept();
                return;
            }
            //A new socket was created by the AcceptAsync method. The SocketAsyncEventArgs object which did the accept operation has that 
            //socket info in its AcceptSocket property. Now we will give a reference for that socket to the SocketAsyncEventArgs object which will do receive/send.
            ((Connection)acceptEventArgs.UserToken).PrepareForRecvSend();
            ((Connection)acceptEventArgs.UserToken).ReInit(true, 4);//clear all internal states to default values for new connection
            ((Connection)acceptEventArgs.UserToken).client.PrivateKey = Program.loginKey;
            Output.WriteLine(ConsoleColor.Gray, "LoginConnectionListener::ProcessAccept LoginServer connected : " + ((Connection)acceptEventArgs.UserToken).ConnectionIP);
            StartReceive((Connection)acceptEventArgs.UserToken);
            LoginServerInterface.LoginServerConnection = (Connection)acceptEventArgs.UserToken;
            // if its look like OK connection pass it forward
            StartAccept();
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error. It might be better to put the code
            //in the ProcessAccept method.
            if (e.AcceptSocket != null && e.AcceptSocket.Connected) ProcessAccept(e);
        }

        // This method is called whenever a receive or send operation completes.
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error.
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    //This exception will occur if you code the Completed event of some
                    //operation to come to this method, by mistake.
                    throw new ArgumentException("LoginConnectionListener::IO_Completed " + "The last operation completed on the socket was not a receive or send");
            }
        }

        // Set the receive buffer and post a receive op.
        private void StartReceive(Connection re)
        {
            // Post async receive operation on the socket.
            bool willRaiseEvent = false;
            try
            {
                willRaiseEvent = re.RecvSocket.AcceptSocket.ReceiveAsync(re.RecvSocket);
            }
            catch (ObjectDisposedException)
            {
                HandleBadConnection(re);
                willRaiseEvent = true;
            }
            catch (NullReferenceException)
            {
                HandleBadConnection(re);
                willRaiseEvent = true;
            }
            if (!willRaiseEvent)
            {
                //If the op completed synchronously, we need to call ProcessReceive method directly.
                ProcessReceive(re.RecvSocket);
            }
        }

        // This method is invoked by the IO_Completed method when an asynchronous receive operation completes. 
        private void ProcessReceive(SocketAsyncEventArgs re)
        {
            // If there was a socket error, close the connection. This is NOT a normal situation, if you get an error here.
            if (re.SocketError != SocketError.Success)
            {
                Output.WriteLine(ConsoleColor.Red, "LoginConnectionListener::ProcessReceive " + "Receive ERROR");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            // If no data was received, close the connection. This is a NORMAL situation that shows when the client has finished sending data = closed connection
            if (re.BytesTransferred == 0)
            {
                Output.WriteLine("LoginConnectionListener::ProcessReceive " + "Receive NO DATA");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            //The BytesTransferred property tells us how many bytes we need to process.
            Int32 remainingBytesToProcess = re.BytesTransferred;
            //Output.WriteLine("RECV packet length: " + remainingBytesToProcess.ToString());
            int status = ((Connection)re.UserToken).Recv(remainingBytesToProcess);
            if (status == -1)// ther was criticall error in recv packet
            {
                Output.WriteLine("LoginConnectionListener::ProcessReceive Error in packet processing - close connection");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            //wait for next packet
            StartReceive((Connection)re.UserToken);
        }

        // This method is called by I/O Completed() when an asynchronous send completes.  
        private void ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            if (sendEventArgs.SocketError == SocketError.Success)
            {
                // If some of the bytes in the message have NOT been sent,
                // then we will need to post another send operation, after we store
                // a count of how many bytes that we sent in this send op.                    
                //receiveSendToken.bytesSentAlreadyCount += receiveSendEventArgs.BytesTransferred;
                // So let's loop back to StartSend().
                //StartSend(receiveSendEventArgs);
                //}
            }
            else
            {
                //If we are in this else-statement, there was a socket error.
                Output.WriteLine(ConsoleColor.Red, "LoginConnectionListener::ProcessSend " + "ProcessSend ERROR");
                // We'll just close the socket if there was a socket error when receiving data from the client.
                HandleBadConnection((Connection)sendEventArgs.UserToken);
            }
        }

        private void HandleBadAccept(Connection con)
        {
            con.Close();
            LoginServerInterface.LoginServerConnection = null;
            this.theMaxConnectionsEnforcer.Release();
        }

        private void HandleBadConnection(Connection con)
        {
            con.Close();
            LoginServerInterface.LoginServerConnection = null;
            this.theMaxConnectionsEnforcer.Release();
        }

        public void CleanUpOnExit()
        {
            DisposeAllSaeaObjects();
        }

        private void DisposeAllSaeaObjects()
        {
            //stop listening port
            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
                listenSocket.Close();
            }
            LoginServerInterface.LoginServerConnection = null;
            listenSocket.Dispose();
            loginServerConnection.Close();
            loginServerConnection.Dispose();
        }

    }
}
