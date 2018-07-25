using System;
using System.Collections.Generic;
using System.Text;

namespace ScClient
{
    public interface IJsonConverter
    {
        string Serialize(object model);
        T Deserialize<T>(string json);
    }
}
