package org.firstinspires.ftc.robotcontroller.internal;

import com.qualcomm.robotcore.eventloop.opmode.OpMode;

import org.firstinspires.ftc.robotcore.external.Telemetry;

import java.io.BufferedWriter;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.Queue;
import java.util.concurrent.locks.ReentrantLock;

public class TelemetryPCLog {

    private ServerSocket serverSocket;
    private Thread serverThread;
    private static final int SERVERPORT = 8333;

    private List<CommunicationThread> currentUsers = new ArrayList<>();
    ReentrantLock writeLock = new ReentrantLock(true);


    public void init() {
        // Start the server
        this.serverThread = new Thread(new ServerThread());
        this.serverThread.start();
    }

    public void Broadcast(String message)
    {
        for (CommunicationThread user: currentUsers) {
            try {
                writeLock.lock();
                try {
                    user.outputQueue.add(message);
                }finally {
                    writeLock.unlock();
                }
            }
            catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

    class ServerThread implements Runnable {
        public void run() {
            Socket socket;
            try {
                // assign the socket in TelemetryPCLog for future use
                serverSocket = new ServerSocket(SERVERPORT);
            } catch (IOException e) {
                e.printStackTrace();
            }
            while (!Thread.currentThread().isInterrupted()) {
                try {
                    socket = serverSocket.accept();

                    CommunicationThread commThread = new CommunicationThread(socket);
                    currentUsers.add(commThread);
                    new Thread(commThread).start();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }
    }

    class CommunicationThread implements Runnable {
        private Socket clientSocket;
        private BufferedWriter output;

        public Queue<String> outputQueue;

        public CommunicationThread(Socket clientSocket) {
            outputQueue = new LinkedList<>();

            this.clientSocket = clientSocket;

            try {
                this.output = new BufferedWriter(new OutputStreamWriter(this.clientSocket.getOutputStream()));
            } catch (IOException e) {
                e.printStackTrace();
            }
        }

        public void run() {
            while (!Thread.currentThread().isInterrupted()) {
                try {
                    if(outputQueue.size() > 0) {
                        writeLock.lock();
                        try {
                            output.write(outputQueue.remove());
                            output.flush();
                        }finally {
                            writeLock.unlock();
                        }

                    }
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }

    }
}


