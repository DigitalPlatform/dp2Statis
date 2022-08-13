using dp2Statis.Reporting;

namespace UnitTestStatis
{
    [TestClass]
    public class UnitTestReplication
    {
        [TestMethod]
        public void Test_getMinValue_01()
        {
            // ���� Utc Kind �� MinValue ���� MinValue
            var value = DataUtility.GetMinValue();
            Assert.AreEqual(DateTimeKind.Utc, value.Kind);
            Assert.AreEqual(DateTime.MinValue, value);
        }
    }
}