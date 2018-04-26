namespace DMSSocketReceiver.dmshandler
{
    public class DMSDocument
    {
        public string name { private set; get; }

        public string id { private set;  get; }

        public DMSDocument(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString()
        {
            return string.Format("doc '{1}' (id {0})", this.id, this.name);
        }
    }
}
