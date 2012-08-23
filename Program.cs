using System;
using System.Net;
using System.Web;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Configuration;

namespace AGOLFeatureServiceUpdater
{
    class Program
    {
        private static LicenseInitializer aoLicenseInitializer = new AGOLFeatureServiceUpdater.LicenseInitializer();
        private static String pathToLocalFileGDB;
        private static String apiBase;

        static void Main(string[] args)
        {
            try
            {
                pathToLocalFileGDB = ConfigurationManager.AppSettings.Get("LocalFileGDBPath").ToString();
                apiBase = ConfigurationManager.AppSettings.Get("AGOLAPIBase").ToString();

                DeleteAllFeatureFromFeatureService();

                aoLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeArcView },
                new esriLicenseExtensionCode[] { });
            
                IWorkspaceFactory workspaceFactory = new ESRI.ArcGIS.DataSourcesGDB.FileGDBWorkspaceFactoryClass();
                IFeatureWorkspace ws = (IFeatureWorkspace)workspaceFactory.OpenFromFile(pathToLocalFileGDB, 0);
                IFeatureClass recSitesFeatureClass = ws.OpenFeatureClass("wenatchee_rec_sites");
                int typeFieldIndex = recSitesFeatureClass.FindField("TYPE");
                int areaFieldIndex = recSitesFeatureClass.FindField("AREA");
                int perimeterFieldIndex = recSitesFeatureClass.FindField("PERIMETER");
                int nameFieldIndex = recSitesFeatureClass.FindField("REC_NAME");
                //string featureJSONArrayString = "[";
                IFeatureCursor fcur = recSitesFeatureClass.Search(null, false);
                IFeature recFeature = fcur.NextFeature();
                while (recFeature != null)
                {
                    IPoint pt = (IPoint)recFeature.Shape;
                    string jsonFeature = string.Empty;
                    jsonFeature += "[{'geometry': {'x': " + pt.X + ",'y': " + pt.Y + ",'spatialReference': {'wkid': 4326}},'attributes': {'TYPE': '";
                    jsonFeature += recFeature.get_Value(typeFieldIndex) + "','AREA': '" + recFeature.get_Value(areaFieldIndex);
                    jsonFeature += "','PERIMETER': '" + "','REC_NAME': '" + recFeature.get_Value(nameFieldIndex) + "'}}]";

                    //string jsonf = "[{'geometry': {'x': -120.663242,'y': 47.784908,'spatialReference': {'wkid': 4326}},"
                     //+ "'attributes': {'TYPE': 'Picnic Area','AREA': '0','PERIMETER': '0','REC_NAME': 'Chumstick!!!'}}]";

                    string reqString = apiBase + "/WenatcheeNationalForestRecSites/FeatureServer/0/addFeatures?f=json&features=" + jsonFeature;
                    HttpWebRequest req = WebRequest.Create(new Uri(reqString)) as HttpWebRequest;
                    req.Method = "POST";
                    req.ContentType = "application/json";

                    // Encode the parameters as form data:
                    byte[] formData = UTF8Encoding.UTF8.GetBytes(reqString);
                    // Send the request:
                    using (Stream post = req.GetRequestStream())
                    {
                        post.Write(formData, 0, formData.Length);
                    }
                    // Pick up the response:
                    string result = null;
                    using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader =
                            new StreamReader(resp.GetResponseStream());
                        result = reader.ReadToEnd();
                        Console.WriteLine(result.ToString());
                    }
                    recFeature = fcur.NextFeature();
                }
            }
            catch(Exception e)
            {
                string ouch = e.Message;
                return;
            }
            finally
            {
            aoLicenseInitializer.ShutdownApplication();
            }
        }
        static Boolean DeleteAllFeatureFromFeatureService()
        {
            try
            {
                string reqString = apiBase + "/WenatcheeNationalForestRecSites/FeatureServer/0/deleteFeatures?f=json&where=OBJECTID > -1";
                HttpWebRequest req = WebRequest.Create(new Uri(reqString)) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/json";

                // Encode the parameters as form data:
                byte[] formData = UTF8Encoding.UTF8.GetBytes(reqString);
                //req.contentLength = formData.Length;

                // Send the request:
                using (Stream post = req.GetRequestStream())
                {
                    post.Write(formData, 0, formData.Length);
                }

                // Pick up the response:
                string result = null;
                using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader =
                        new StreamReader(resp.GetResponseStream());
                    result = reader.ReadToEnd();
                    Console.WriteLine(result.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
                //throw;
            }
        }
    }
}
