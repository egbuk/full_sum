using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace HalfsSum
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DataTable dt = new DataTable();

        private void Form1_Load(object sender, EventArgs e)
        {
            DataColumn dCol = dt.Columns.Add();
            dCol.ColumnName = "colCode";
            dCol.Caption = "Код т.у.";
            dCol.DataType = typeof(string);

            dCol = dt.Columns.Add();
            dCol.ColumnName = "colSumm1";
            dCol.Caption = "Сумма А+ Канал 01";
            dCol.DataType = typeof(string);

            dCol = dt.Columns.Add();
            dCol.ColumnName = "colCount1";
            dCol.Caption = "Кол-во часов A+ ";
            dCol.DataType = typeof(string);


            dCol = dt.Columns.Add();
            dCol.ColumnName = "colSumm3";
            dCol.Caption = "Сумма R+ Канал 03";
            dCol.DataType = typeof(string);


            dCol = dt.Columns.Add();
            dCol.ColumnName = "colCount3";
            dCol.Caption = "Кол-во часов R+";
            dCol.DataType = typeof(string);

            dCol = dt.Columns.Add();
            dCol.ColumnName = "colCountXml";
            dCol.Caption = "Кол-во файлов xml";
            dCol.DataType = typeof(string);

            DataRow dr = dt.Rows.Add();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                dr[i] = dt.Columns[i].Caption;
            }


            dataGridView1.DataSource = dt;
        }

        List<FileInfo> fiList = new List<FileInfo>();
        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                fiList.Clear();

                string[] files = Directory.GetFiles(folderBrowserDialog1.SelectedPath);
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo fi = new FileInfo(files[i]);
                    fiList.Add(fi);
                }

                dt.Clear();
                DataRow dr = dt.Rows.Add();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dr[i] = dt.Columns[i].Caption;
                }
                parseXmlFiles();
            }
        }

        public struct MeasuringsByMeter
        {
            public double sum1;
            public int count1;
            public double sum3;
            public int count3;
            public int countXml;
            public string meterName;
        }

        Dictionary<string, MeasuringsByMeter> measuringsDict = new Dictionary<string, MeasuringsByMeter>();

        private void sumForMeasuringPoint(XmlDocument xDoc, XmlNode measuringPointNode, out MeasuringsByMeter measForOneMesPoint)
        {
            measForOneMesPoint = new MeasuringsByMeter();
            //TOOD: а вдруг элемент
            XmlNodeList channelsList = measuringPointNode.ChildNodes;
            
            for (int i = 0; i < channelsList.Count; i++)
            {
                XmlNode tmpNode = channelsList[i];
                XmlNode codeAttribObj = tmpNode.Attributes.GetNamedItem("code");

                if (codeAttribObj == null || (codeAttribObj.Value != "01" && codeAttribObj.Value != "03")) continue;

                XmlNodeList periodsList = tmpNode.ChildNodes;


                string tmpValStr = "";
                int tmpCount = 0;
                double tmpSum = 0;
                
                foreach (XmlNode period in periodsList)
                {
                    XmlNode n = period.FirstChild;
                    
                    if (n != null)
                    { 
                        double tmpD = 0;
                        tmpValStr = n.InnerText;

                        try
                        {
                            tmpD = double.Parse(tmpValStr, System.Globalization.CultureInfo.InvariantCulture);
                            tmpSum += tmpD;
                            tmpCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("sumForMeasuringPoint Ошибка преобразования: " + ex + "; " + tmpValStr);
                        }
                    }
                }

                if ( codeAttribObj.Value == "01")
                {
                    measForOneMesPoint.count1 = tmpCount;
                    measForOneMesPoint.sum1 = tmpSum;
                }
                else if (codeAttribObj.Value == "03")
                {
                    measForOneMesPoint.count3 = tmpCount;
                    measForOneMesPoint.sum3 = tmpSum;
                }
            }
        } 

        private void addRowsToTable(Dictionary<string, MeasuringsByMeter> dict)
        {
            string[] keys = dict.Keys.ToArray<string>();
            foreach (string k in keys)
            {
                MeasuringsByMeter m = dict[k];
                DataRow dr = dt.Rows.Add();
                dr["colCode"] = k;
                dr["colSumm1"] = m.sum1;
                dr["colCount1"] = m.count1;
                dr["colSumm3"] = m.sum3;
                dr["colCount3"] = m.count3;
                dr["colCountXml"] = m.countXml;
            }
        }
        
        private void parseXmlFiles()
        {
            measuringsDict.Clear();

            for (int i = 0; i < fiList.Count; i++)
            {
                if (fiList[i].Extension != ".xml") continue;

                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(fiList[i].FullName);

                XmlNodeList measuringPoints = xDoc.GetElementsByTagName("measuringpoint");
  

                for (int j = 0; j < measuringPoints.Count; j++)
                {
                    XmlNode curMeasuringPoint = measuringPoints[j];
                    XmlNode mPointCodeAttr = curMeasuringPoint.Attributes.GetNamedItem("code");
                    string mPointCodeStr = "";
                    if (mPointCodeAttr != null)
                        mPointCodeStr = mPointCodeAttr.Value;

                    MeasuringsByMeter tmpMbm = new MeasuringsByMeter();
                    sumForMeasuringPoint(xDoc, curMeasuringPoint, out tmpMbm);

                    if (measuringsDict.ContainsKey(mPointCodeStr))
                    {
                        MeasuringsByMeter mbm = measuringsDict[mPointCodeStr];
                        mbm.count1 += tmpMbm.count1;
                        mbm.count3 += tmpMbm.count3;
                        mbm.sum1 += tmpMbm.sum1;
                        mbm.sum3 += tmpMbm.sum3;
                        mbm.countXml++;

                        measuringsDict[mPointCodeStr] = mbm;
                    }
                    else
                    {
                        MeasuringsByMeter mbm = new MeasuringsByMeter();
                        mbm.meterName = curMeasuringPoint.Attributes.GetNamedItem("name").Value;

                        mbm.count1 = tmpMbm.count1;
                        mbm.count3 = tmpMbm.count3;
                        mbm.sum1 = tmpMbm.sum1;
                        mbm.sum3 = tmpMbm.sum3;
                        mbm.countXml++;

                        measuringsDict.Add(mPointCodeStr, mbm);
                    }
                }
            }


            addRowsToTable(measuringsDict);
        }
    }
}
