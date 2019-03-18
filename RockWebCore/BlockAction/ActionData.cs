using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace RockWebCore.BlockAction
{
    public class ActionData
    {
        IDictionary<string, object> State { get; set; }

        public IDictionary<string, JToken> Parameters { get; set; }

        public ActionData()
        {
            State = new Dictionary<string, object>();
            Parameters = new Dictionary<string, JToken>();
        }
    }
}
