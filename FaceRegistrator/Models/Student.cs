using Newtonsoft.Json;

namespace FaceRegistrator.Models
{
    public class Student
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("face")]
        public int IsFaceRegistred { get; set; }

        public Student(int id, string fullname, int isFaceRegistred)
        {
            ID = id;
            Fullname = fullname;
            IsFaceRegistred = isFaceRegistred;
        }

        public override string? ToString()
        {
            return $"{Fullname} (ID: {ID}, Status: {(IsFaceRegistred == 1 ? "OK" : "NO")})";
        }
    }
}
