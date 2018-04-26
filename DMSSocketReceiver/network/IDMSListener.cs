namespace DMSSocketReceiver
{

    public interface IDMSListener
    {

        void StartListening(int port);
        void StopListening();

    }
}