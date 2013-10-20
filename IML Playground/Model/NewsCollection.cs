using IML_Playground.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Model
{
    class NewsCollection : Collection<NewsItem>
    {
        public static NewsCollection CreateFromZip(string path)
        {
            NewsCollection nc = new NewsCollection();

            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Error: file '{0}' not found.", path);
                return null;
            }

            using (ZipArchive zip = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    NewsItem item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);
                    if (item != null)
                        nc.Add(item);
                }
            }

            return nc;
        }
    }
}
