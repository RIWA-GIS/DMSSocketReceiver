namespace DMSSocketReceiver.dmshandler
{
    public class DMSDocument
    {
        public string Name { private set; get; }

        public string Id { private set;  get; }

        public DMSDocument(int id, string name) : this(id.ToString(), name) { }

        public DMSDocument(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public override string ToString()
        {
            return string.Format("doc '{1}' (id {0})", this.Id, this.Name);
        }
    }
}
