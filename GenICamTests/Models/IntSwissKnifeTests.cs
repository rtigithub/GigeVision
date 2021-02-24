using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam.Tests
{
    [TestClass()]
    public class IntSwissKnifeTests
    {
        [TestMethod()]
        public void ExecuteFormulaTest()
        {
            string formula = "30*(VAR_PLC_PG0_GRANULARITYFACTOR+1 )*(VAR_PLC_PG0_WIDTH+VAR_PLC_PG0_DELAY+1)";
            Dictionary<string, object> pVaribles = new Dictionary<string, object>();
            pVaribles.Add("VAR_PLC_PG0_GRANULARITYFACTOR", new GenInteger(10));
            pVaribles.Add("VAR_PLC_PG0_WIDTH", new GenInteger(5));
            pVaribles.Add("VAR_PLC_PG0_DELAY", new GenInteger(20));
            IntSwissKnife intSwissKnife = new IntSwissKnife(formula, pVaribles);
            Assert.AreEqual(8580, Int64.Parse(intSwissKnife.Value.Result.ToString()));

            formula = "((ROUND(((0.1*(2**7))-0.5),0))/(2**7))";
            intSwissKnife = new IntSwissKnife(formula, null);
            Assert.AreEqual(0.09375, double.Parse(intSwissKnife.Value.Result.ToString()));

            formula = " (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0x8 )  ?  ( 0 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0xA )  ?  ( 1 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0xC )  ?  ( 2 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0xE )  ?  ( 3 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0x10 )  ?  ( 4 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0x18 )  ?  ( 5 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0x20 )  ?  ( 6 )  :  (  (  (  ( 17301512 & 0x00ff0000 )  >  > 16 )  = 0x40 )  ?  ( 7 )  :  ( 8 )  )  )  )  )  )  )  )  ) ";
            intSwissKnife = new IntSwissKnife(formula, null);
            Assert.AreEqual(0.09375, double.Parse(intSwissKnife.Value.Result.ToString()));
        }
    }
}