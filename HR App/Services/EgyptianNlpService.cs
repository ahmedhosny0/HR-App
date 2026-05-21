using HR_App.Models;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace HR_App.Services
{
    public class EgyptianNlpService
    {
        public string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Trim();
            text = Regex.Replace(text, @"\s+", " ");

            return text;
        }

        // 🔥 Extract anything dynamic (not hardcoded meaning mapping)
        public List<AiEntity> ExtractEntities(string text)
        {
            var entities = new List<AiEntity>();

            // simple heuristic: nouns (can be replaced later by LLM)
            var words = text.Split(' ');

            foreach (var w in words)
            {
                if (w.Length < 2) continue;

                entities.Add(new AiEntity
                {
                    Original = w,
                    Normalized = w,
                    Type = "unknown"
                });
            }

            return entities;
        }
    }
}