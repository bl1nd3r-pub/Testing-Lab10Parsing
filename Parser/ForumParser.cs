using HtmlAgilityPack;
using ParserDb;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Parser
{
    public class ForumParser
    {
        private readonly PostRepository _repository;

        public ForumParser(PostRepository repository)
        {
            _repository = repository;
        }

        public async Task ParseAndSaveAsync(string htmlFilePath)
        {
            // 1. Загрузка документа
            var doc = new HtmlDocument();
            doc.Load(htmlFilePath, System.Text.Encoding.UTF8);

            // 2. Выбор всех узлов с классом "post"
            // XPath: //div[@class='post'] ищет div с указанным классом на любом уровне вложенности
            var postNodes = doc.DocumentNode.SelectNodes("//div[@class='post']");
            if (postNodes == null) return;

            foreach (var node in postNodes)
            {
                try {
                    var idAttr = node.GetAttributeValue("data-id", "0");
                    if (!int.TryParse(idAttr, out int id)) continue;

                    var nameNode = node.SelectSingleNode(".//span[contains(@class, 'username')]");
                    string name = nameNode?.InnerText.Trim() ?? "Unknown";

                    var msgNode = node.SelectSingleNode(".//span[contains(@class, 'message')]");
                    string message = nameNode?.InnerText.Trim() ?? string.Empty;

                    await _repository.AddAsync(id, name, message);
                    Console.WriteLine($"[OK] ID={id}, Author={name}");
                }
                catch (Exception ex) {
                    Console.WriteLine($"[WARNING] Пропуск узла: {ex.Message}");
                }
            }
        }
    }
}