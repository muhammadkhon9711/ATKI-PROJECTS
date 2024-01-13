using Newtonsoft.Json;

namespace FaceRegistrator.Models
{
    public class Group
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public Group(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public override string? ToString()
        {
            return $"{Name} (ID: {ID})";
        }
    }
}
