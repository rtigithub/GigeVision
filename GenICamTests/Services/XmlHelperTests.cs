using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using GigeVision.Core.Models;
using GigeVision.Core;

namespace GenICam.Tests
{
    [TestClass()]
    public class XmlHelperTests
    {
        [TestMethod()]
        public void GetStringCategoryTest()
        {
        }

        [TestMethod()]
        public void XmlHelperTest()
        {
            //XmlDocument xml = new XmlDocument();
            ////xml.Load("CXG_IP_rev03000034_190717.xml");

            Gvcp gvcp = new Gvcp("192.168.10.244");
            var genPort = new GenPort(gvcp);

            // XmlHelper xmlHelper = new XmlHelper("Category", xml, genPort);
            //var width = xmlHelper.CategoryDictionary["ImageSizeControl"].PFeatures["Width"] as GenInteger;

            //var value = width.GetValue();
        }
    }
}