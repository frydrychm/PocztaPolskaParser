using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PostOfficeParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("podaj nazwę pliku");
                return;
            }
            using (PdfReader reader = new PdfReader(args[0]))
            {
                PdfDocument pdf = new PdfDocument(reader);
                FilteredEventListener listener = new FilteredEventListener();
                var strat = listener.AttachEventListener(new MyLocationTextExtractionStrategy());
                PdfCanvasProcessor processor = new PdfCanvasProcessor(listener);
                List<TextItem[]> rows = new List<TextItem[]>();
                int count = pdf.GetNumberOfPages();
                int pagesCount = 1633;
                if (args.Length == 2)
                {
                    pagesCount = int.Parse(args[1]);
                }
                for (int i = 1; i <= count; i++)
                {
                    if (i > pagesCount)
                    {
                        break;
                    }
                    Console.WriteLine("parsowanie strony " + i.ToString() + " z " + count.ToString());
                    processor.ProcessPageContent(pdf.GetPage(i));
                    foreach (var row in strat.rows)
                    {
                        int countPNA = row.Value.Where(o => o.text.LastIndexOf("PNA miejscowości") >= 0).Count();
                        if (countPNA > 0)
                        {
                            continue;
                        }
                        int countPage = row.Value.Where(o => o.text.LastIndexOf("Oficjalny Spis") >= 0).Count();
                        if (countPage > 0)
                        {
                            continue;
                        }
                        countPage = row.Value.Where(o => o.text.LastIndexOf("Strona") >= 0).Count();
                        if (countPage > 0)
                        {
                            continue;
                        }
                        countPage = row.Value.Where(o => o.text.LastIndexOf("Część") >= 0).Count();
                        if (countPage > 0)
                        {
                            continue;
                        }
                        if (row.Value.Count < 5)
                        {
                            foreach (TextItem item in row.Value)
                            {
                                var temp = rows[rows.Count - 1].Where(x => Math.Abs(x.position - item.position) <= 1);
                                if (temp.Count() > 0)
                                {
                                    rows[rows.Count - 1].Where(x => Math.Abs(x.position - item.position) <= 1).ToArray()[0].text += item.text;
                                }
                            }
                            continue;
                        }
                        var items = row.Value.OrderBy(o => o.position).ToArray();
                        rows.Add(row.Value.ToArray());

                    }
                    strat.rows.Clear();
                }
                Console.WriteLine("\n");
                List<string> results = new List<string>();
                List<int> columns = new List<int>();
                int index = 0;
                foreach (TextItem[] row in rows)
                {
                    Console.Write("\r" + (index * 100 / rows.Count));
                    index++;
                    if (row.Length == 7)
                    {
                        columns.Clear();
                        foreach (TextItem item in row)
                        {
                            columns.Add(item.position);
                        }
                        columns = columns.OrderBy(x => x).ToList();
                    }
                    else
                    {
                        List<string> temp = new List<string>();
                        foreach (int column in columns)
                        {
                            var item2 = row.Where(x => Math.Abs(x.position - column) <= 1).ToArray();
                            temp.Add((item2.Length > 0) ? item2[0].text.Trim() : "");
                        }
                        if (temp.Count() > 0)
                        {
                            results.Add(string.Join(';', temp.ToArray()));
                        }

                    }
                }
                File.WriteAllText("spis.csv", string.Join('\n', results));
            }

        }
        public class textChunk
        {
            public string text { get; set; }
            public Rectangle rect { get; set; }
            public string fontFamily { get; set; }
            public float fontSize { get; set; }
            public float spaceWidth { get; set; }
        }
        public class TextItem
        {
            public string text { get; set; }
            public int position;
        }
        public class MyLocationTextExtractionStrategy : LocationTextExtractionStrategy
        {
            //Hold each coordinate
            public Dictionary<int, List<TextItem>> rows = new Dictionary<int, List<TextItem>>();


            public override void EventOccurred(IEventData data, EventType type)
            {
                if (!type.Equals(EventType.RENDER_TEXT))
                    return;

                TextRenderInfo renderInfo = (TextRenderInfo)data;

                IList<TextRenderInfo> text = renderInfo.GetCharacterRenderInfos();
                string line = "";
                if (text.Count == 0)
                {
                    return;
                }
                Vector position = text[0].GetBaseline().GetStartPoint();
                foreach (TextRenderInfo t in text)
                {
                    string letter = t.GetText();
                    line += letter;
                }
                int y = (int)Math.Round(position.Get(1));
                int x = (int)Math.Round(position.Get(0));
                if (rows.ContainsKey(y))
                {
                    rows[y].Add(new TextItem() { position = x, text = line });
                }
                else if (rows.ContainsKey(y + 1))
                {
                    rows[y + 1].Add(new TextItem() { position = x, text = line });
                }
                else if (rows.ContainsKey(y - 1))
                {
                    rows[y - 1].Add(new TextItem() { position = x, text = line });
                }
                else
                {
                    rows[y] = new List<TextItem>();
                    rows[y].Add(new TextItem() { position = x, text = line });
                }
            }
        }
    }
}
