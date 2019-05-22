using System;

namespace DMSSocketReceiver
{

    public interface IDMSListener
    {

        void StartListening(int port);
        void StopListening();
        event EventHandler CommandReceivedEvent;
        event EventHandler CommandFinishedEvent;
        event EventHandler CommandErrorEvent;
    }
}