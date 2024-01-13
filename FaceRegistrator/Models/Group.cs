using Newtonsoft.Json;

namespace FaceRegistrator.Models
{
    public class Group
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("course")]
        public short Course { get; set; }
        public Group(int id, string name, short course)
        {
            ID = id;
            Name = name;
            Course = course;
        }

        public override string? ToString()
        {
            return $"{Course} - kurs, {Name} guruh (ID: {ID})";
        }
    }
}
