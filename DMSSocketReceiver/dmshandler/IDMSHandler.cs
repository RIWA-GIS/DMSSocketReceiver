using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMSSocketReceiver.dmshandler
{
    public interface IDMSHandler
    {
        DMSCapabilities DetermineCapabilities();
        void ShowDocument(DMSDocument document);
        DMSDocument InsertDocument(string originKey, string filepath, IDictionary<string, string> metadata);
        DMSDocument AttachDocument(string originKey, IDictionary<string, string> metadata);
    }
}
