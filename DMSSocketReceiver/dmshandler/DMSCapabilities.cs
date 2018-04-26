namespace DMSSocketReceiver.dmshandler
{
    public class DMSCapabilities
    {
        public bool insertable { private set; get; }
        public bool attachable { private set; get; }

        public DMSCapabilities(bool insertable, bool attachable)
        {
            this.insertable = insertable;
            this.attachable = attachable;
        }
    }
}
