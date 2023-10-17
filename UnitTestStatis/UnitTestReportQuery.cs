
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DigitalPlatform.LibraryServer.Reporting.Report;

using static MoreLinq.Extensions.LeftJoinExtension;
using MoreEnumerable = MoreLinq.MoreEnumerable;

using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.Marc;
using DigitalPlatform.Text;

namespace UnitTestStatis
{
    [TestClass]
    public class UnitTestReportQuery
    {
        LibraryContext NewContext()
        {
            var config = new DatabaseConfig
            {
                Testing = true
            };
            
            var context = new LibraryContext(config);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        void AddOperLog(LibraryContext context,
            string action = "borrow",
            string itemBarcode = "0000001")
        {
            context.CircuOpers.Add(new CircuOper
            {
                Action = action,
                Date = "20230101",
                No = 1,
                OperTime = new DateTime(2023, 1, 1,
8, 0, 0),
                ReturningTime = new DateTime(2023, 2, 1,
8, 0, 0),
                ItemBarcode = itemBarcode
            });
        }

        void AddItem(LibraryContext context,
            string itemBarcode = "0000001",
            string itemRecPath = "中文图书实体/1",
            string biblioRecPath = "中文图书/1")
        {
            context.Items.Add(new Item
            {
                ItemBarcode = itemBarcode,
                Location = "/阅览室",
                ItemRecPath = itemRecPath,
                BiblioRecPath = biblioRecPath
            });
        }

        void AddBiblio(LibraryContext context,
            string biblioRecPath = "中文图书/1")
        {
            context.Biblios.Add(new Biblio
            {
                RecPath = biblioRecPath,
                Summary = "图书的书名"
            });
        }


        // parameters:
        //      style   operlog,item,biblio 的一个或者多个组合
        LibraryContext BuildDatabase(string style)
        {
            var config = new DatabaseConfig
            {
                Testing = true
            };
            var context = new LibraryContext(config);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.SaveChanges();

            if (StringUtil.IsInList("operlog", style) == true)
            {
                context.CircuOpers.Add(new CircuOper
                {
                    Action = "borrow",
                    Date = "20230101",
                    No = 1,
                    OperTime = new DateTime(2023, 1, 1,
                8, 0, 0),
                    ReturningTime = new DateTime(2023, 2, 1,
                8, 0, 0),
                    ItemBarcode = "0000001"
                });
            }

            if (StringUtil.IsInList("item", style) == true)
            {
                context.Items.Add(new Item
                {
                    ItemBarcode = "0000001",
                    Location = "/阅览室",
                    ItemRecPath = "中文图书实体/1",
                    BiblioRecPath = "中文图书/1"
                });
            }

            // TestAddOrUpdateBiblio(context);

            if (StringUtil.IsInList("biblio", style) == true)
            {
                context.Biblios.Add(new Biblio
                {
                    RecPath = "中文图书/1",
                    Summary = "图书的书名"
                });
            }

            context.SaveChanges();
            return context;
        }

        static void TestAddOrUpdateBiblio(LibraryContext context)
        {
            string recpath = "test/1";
            // 删除可能存在的记录
            var record = context.Biblios
                .Where(x => x.RecPath == recpath).FirstOrDefault();
            if (record != null)
            {
                context.Biblios.Remove(record);
                context.SaveChanges();
            }

            // 创建一条新的记录
            {
                MarcRecord marc = new MarcRecord();
                marc.add(new MarcField('$', "200  $atitle1$fauthor1"));
                marc.add(new MarcField('$', "690  $aclass_string1"));
                Biblio biblio = new Biblio { RecPath = recpath };
                if (MarcUtil.Marc2Xml(marc.Text,
        "unimarc",
        out string xml,
        out string error) == -1)
                    throw new Exception(error);
                biblio.RecPath = recpath;
                biblio.Xml = xml;
                biblio.Create(biblio.Xml, biblio.RecPath);
                context.Add(biblio);
                context.SaveChanges();
            }

            // 用于最后阶段比对检索点
            List<Key> save_keys = new List<Key>();

            // 更新上述书目记录
            {
                MarcRecord marc = new MarcRecord();
                marc.add(new MarcField('$', "200  $atitle2$fauthor2"));
                marc.add(new MarcField('$', "690  $aclass_string2"));
                if (MarcUtil.Marc2Xml(marc.Text,
        "unimarc",
        out string xml,
        out string error) == -1)
                    throw new Exception(error);

                var biblio = context.Biblios.SingleOrDefault(c => c.RecPath == recpath)
    ?? new Biblio { RecPath = recpath };

                Debug.Assert(biblio.RecPath == recpath);

                biblio.RecPath = recpath;
                biblio.Xml = xml;
                biblio.Create(biblio.Xml, biblio.RecPath);

                save_keys.AddRange(biblio.Keys);

                context.AddOrUpdate(biblio);
                context.SaveChanges();
            }
        }


        // 测试 201 表的查询过程
        [TestMethod]
        public void Test_query_201_01()
        {
            using var context = BuildDatabase("operlog,item,biblio");

            var param_table = new Hashtable();
            param_table["location"] = "/阅览室";

            var macro_table = new Hashtable();
            string strStartDate = "20230101";
            string strEndDate = "20231231";
            var items = Report.Query_201(context,
param_table,
strStartDate,
strEndDate,
macro_table);
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("图书的书名", items[0].Summary);
        }

        // 缺 item 记录
        [TestMethod]
        public void Test_query_201_02()
        {
            using var context = BuildDatabase("operlog,biblio");

            var param_table = new Hashtable();
            param_table["location"] = "/阅览室";

            var macro_table = new Hashtable();
            string strStartDate = "20230101";
            string strEndDate = "20231231";
            var items = Report.Query_201(context,
param_table,
strStartDate,
strEndDate,
macro_table);
            Assert.AreEqual(0, items.Count());
        }

        // 缺 biblio 记录
        [TestMethod]
        public void Test_query_201_03()
        {
            using var context = BuildDatabase("operlog,item");

            var param_table = new Hashtable();
            param_table["location"] = "/阅览室";

            var macro_table = new Hashtable();
            string strStartDate = "20230101";
            string strEndDate = "20231231";
            var items = Report.Query_201(context,
param_table,
strStartDate,
strEndDate,
macro_table);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(null, items[0].Summary);
        }

        // 测试 202 表的查询过程
        [TestMethod]
        public void Test_query_202_01()
        {
            using var context = NewContext();
            AddOperLog(context);
            AddBiblio(context);
            AddItem(context, itemBarcode: "0000001", itemRecPath: "中文图书实体/1");
            AddItem(context, itemBarcode: "0000002", itemRecPath: "中文图书实体/2");
            context.SaveChanges();

            var param_table = new Hashtable();
            param_table["location"] = "/阅览室";

            var macro_table = new Hashtable();
            string strStartDate = "20230101";
            string strEndDate = "20231231";
            var items = Report.Query_202(context,
param_table,
strStartDate,
strEndDate,
macro_table);
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("图书的书名", items[0].Summary);
        }

        [TestMethod]
        public void Test_query_202_02()
        {
            using var context = NewContext();
            AddOperLog(context);
            AddBiblio(context);
            AddItem(context, itemBarcode: "0000001", itemRecPath: "中文图书实体/1");
            AddItem(context, itemBarcode: "0000002", itemRecPath: "中文图书实体/2", "中文图书/2");    // 没有对应的书目记录
            context.SaveChanges();

            var param_table = new Hashtable();
            param_table["location"] = "/阅览室";

            var macro_table = new Hashtable();
            string strStartDate = "20230101";
            string strEndDate = "20231231";
            var items = Report.Query_202(context,
param_table,
strStartDate,
strEndDate,
macro_table);
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual(null, items[0].Summary);
        }


        #region 验证 LeftJoin

        [TestMethod]
        public void Test_leftJoin_01()
        {
            var list1 = new List<TestItem1> {
                new TestItem1 {
                    Date ="20230101",
                    ItemRecPath = "实体库/1"
                },
                new TestItem1 {
                    Date ="20230102",
                    ItemRecPath = "实体库/2"
                },
                new TestItem1 {
                    Date ="20230103",
                    ItemRecPath = "实体库/3"
                },
            };

            var list2 = new List<TestItem2> {
                new TestItem2 {
                    Barcode = "0000001",
                    RecPath = "实体库/1"
                },
                /*
                new TestItem2 {
                    Barcode = "0000002",
                    RecPath = "实体库/2"
                },
                */
                new TestItem2 {
                    Barcode = "0000003",
                    RecPath = "实体库/3"
                },
            };

#if REMOVED
            var results = list1
                .Join(
                list2.DefaultIfEmpty(new TestItem2 { Barcode = "null", RecPath = "null"}),
                item1 => item1.ItemRecPath,
                item2 => item2.RecPath,
                (item1, item2) => new TestItem3
                {
                    Date = item1.Date,
                    Barcode = item2.Barcode
                })
                .ToList();
#endif


            // https://mydevtricks.com/linq-gems-left-outer-join
            // https://cloud.tencent.com/developer/article/1676559
            var results =
            from a in list1
            join b in list2 on a.ItemRecPath equals b.RecPath

            into re
            from r in re.DefaultIfEmpty(new TestItem2
            {
                RecPath = "null path",
                Barcode = "null barcode",
            })

            select new { a.Date, r.Barcode };

            foreach (var result in results)
            {
                Console.WriteLine($"Date={result.Date},Barcode={result.Barcode}");
            }
        }

        [TestMethod]
        public void Test_leftJoin_02()
        {
            var list1 = new List<TestItem1> {
                new TestItem1 {
                    Date ="20230101",
                    ItemRecPath = "实体库/1"
                },
                new TestItem1 {
                    Date ="20230102",
                    ItemRecPath = "实体库/2"
                },
                new TestItem1 {
                    Date ="20230103",
                    ItemRecPath = "实体库/3"
                },
            };

            var list2 = new List<TestItem2> {
                new TestItem2 {
                    Barcode = "0000001",
                    RecPath = "实体库/1"
                },
                /*
                new TestItem2 {
                    Barcode = "0000002",
                    RecPath = "实体库/2"
                },
                */
                new TestItem2 {
                    Barcode = "0000003",
                    RecPath = "实体库/3"
                },
            };

            var results = list1.AsQueryable()
                .LeftOuterJoin2(
                list2.AsQueryable(),
                item1 => item1.ItemRecPath,
                item2 => item2.RecPath,
                (item1, item2) => new 
                {
                    Date = item1.Date,
                    Barcode = item2.Barcode
                })
                .ToList();

            foreach (var result in results)
            {
                Console.WriteLine($"Date={result.Date},Barcode={result.Barcode}");
            }
        }

        [TestMethod]
        public void Test_leftJoin_03()
        {
            var list1 = new List<TestItem1> {
                new TestItem1 {
                    Date ="20230101",
                    ItemRecPath = "实体库/1"
                },
                new TestItem1 {
                    Date ="20230102",
                    ItemRecPath = "实体库/2"
                },
                new TestItem1 {
                    Date ="20230103",
                    ItemRecPath = "实体库/3"
                },
            };

            var list2 = new List<TestItem2> {
                new TestItem2 {
                    Barcode = "0000001",
                    RecPath = "实体库/1"
                },
                /*
                new TestItem2 {
                    Barcode = "0000002",
                    RecPath = "实体库/2"
                },
                */
                new TestItem2 {
                    Barcode = "0000003",
                    RecPath = "实体库/3"
                },
            };

            // MoreEnumerable.LeftJoin(list1,)

            var results = list1
                .LeftJoin(
                list2,
                item1 => item1.ItemRecPath,
                item2 => item2.RecPath,
                item1 => new
                {
                    Date = item1.Date,
                    Barcode = (string?)"not found"
                },
                (item1, item2) => new
                {
                    Date = item1.Date,
                    Barcode = item2.Barcode
                }
                )
                .ToList();

            foreach (var result in results)
            {
                Console.WriteLine($"Date={result.Date},Barcode={result.Barcode}");
            }
        }


        class TestItem1
        {
            public string? Date { get; set; }
            public string? ItemRecPath { get; set; }
        }

        class TestItem2
        {
            public string? Barcode { get; set; }
            public string? RecPath { get; set; }
        }

        public class TestItem3
        {
            public string? Date { get; set; }
            public string? Barcode { get; set; }
        }

        #endregion
    }
}
