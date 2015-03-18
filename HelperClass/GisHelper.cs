// HELPER CLASS WITH USEFULL STUFF
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ElphoGenericTools.Helpers;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ElphoGenericTools
{
    class GisHelper
    {
        public static void SelectedLayerByName(IMap map, string layers)
        {
            int intCount;
            ILayer pLayer;

            IFeatureLayer pFeatureLayer;

            for (intCount = 0; intCount <= map.LayerCount - 1; intCount++)
            {
                if (map.Layer[intCount] is IFeatureLayer)
                {
                    pLayer = map.Layer[intCount];
                    pFeatureLayer = (IFeatureLayer)pLayer;
                    if (layers == pLayer.Name)
                        pFeatureLayer.Selectable = true;
                    else
                        pFeatureLayer.Selectable = false;
                }
            }
        }

        public static bool LayerExists(ICompositeLayer compositeLayer, IMap map, int mapCount, string name)
        {
            for (int i = 0; i < mapCount; i++)
            {
                ILayer layer;
                if (map != null && compositeLayer == null)
                {
                    layer = map.Layer[i];
                }
                else if (compositeLayer != null)
                {
                    layer = compositeLayer.Layer[i];
                }
                else
                {
                    return false;
                }
                ICompositeLayer comLayer;
                if (((comLayer = layer as ICompositeLayer) != null) && ((layer as IGroupLayer) != null))
                {
                    return LayerExists(comLayer, null, comLayer.Count, name);
                }
                else
                {

                    if (layer.Name == name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void CreateLayerList(ICompositeLayer compositeLayer, IMap map, int mapCount, List<ILayer> mapLayers)
        {
            for (int i = 0; i < mapCount; i++)
            {
                ILayer layer;
                if (map != null && compositeLayer == null)
                    layer = map.Layer[i];
                else if (compositeLayer != null)
                    layer = compositeLayer.Layer[i];
                else
                    return;
                ICompositeLayer comLayer;
                if (((comLayer = layer as ICompositeLayer) != null) && ((layer as IGroupLayer) != null))
                    CreateLayerList(comLayer, null, comLayer.Count, mapLayers);
                else
                    mapLayers.Add(layer);
            }
        }

        public static ILayer GetLayer(ICompositeLayer compositeLayer, IMap map, int mapCount, string name)
        {
            for (int i = 0; i < mapCount; i++)
            {
                ILayer layer;
                if (map != null && compositeLayer == null)
                {
                    layer = map.Layer[i];
                }
                else if (compositeLayer != null)
                {
                    layer = compositeLayer.Layer[i];
                }
                else
                {
                    return null;
                }
                ICompositeLayer comLayer;
                if (((comLayer = layer as ICompositeLayer) != null) && ((layer as IGroupLayer) != null))
                {
                    return GetLayer(comLayer, null, comLayer.Count, name);
                }
                else
                {

                    if (layer.Name == name)
                    {
                        return layer;
                    }
                }
            }
            return null;
        }

        public static IEditTemplate GetTemplate(IEditor3 editor3, string currentTemplate)
        {
            for (int i = 0; i < editor3.TemplateCount; i++)
            {
                IEditTemplate editTemplate = editor3.Template[i];
                if (editTemplate.Layer.Name == currentTemplate)
                {
                    return editTemplate;
                }
            }
            return null;
        }

        public static List<IFeature> GetSelectedFeatures(esriGeometryType type, IMap map)
        {
            List<IFeature> Iflist = new List<IFeature>();
            IEnumFeature pEnumFeat = (IEnumFeature)map.FeatureSelection;
            IFeature pfeat = pEnumFeat.Next();

            while (pfeat != null)
            {
                Iflist.Add(pfeat);
                if (pfeat.Shape.GeometryType != type)
                {
                    return null;
                }
                pfeat = pEnumFeat.Next();
            }

            return Iflist;
        }

        public static List<IFeature> GetExtentFeatures(esriGeometryType type, IMap map, IEditor MEditor)
        {
            List<IFeature> Iflist = new List<IFeature>();
            IActiveView activeView = map as IActiveView;

            var editLayers = MEditor as IEditLayers;
            var layer = editLayers.CurrentLayer;
            if (layer != null)
            {
                var pFeatureClass = layer.FeatureClass;

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = activeView.Extent;
                spatialFilter.GeometryField = pFeatureClass.ShapeFieldName;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                List<IFeature> spatialfeatures = new List<IFeature>();
                // Execute the query and iterate through the cursor's results.
                using (ComReleaser comReleaser = new ComReleaser())
                {
                    IFeatureCursor highwayCursor = pFeatureClass.Search(spatialFilter, false);
                    comReleaser.ManageLifetime(highwayCursor);
                    IFeature highwayFeature = null;
                    while ((highwayFeature = highwayCursor.NextFeature()) != null)
                    {
                        spatialfeatures.Add(highwayFeature);
                    }
                }

                foreach (var spatialfeature in spatialfeatures)
                {
                    if (spatialfeature.Shape.GeometryType != type)
                    {
                        return null;
                    }
                    else
                    {
                        Iflist.Add(spatialfeature);
                    }
                }

                return Iflist;
            }
            else
            {
                MessageBox.Show("You are not in editing mode");
            }
            return null;
        }

        public static IElement AddTextToMap(IPoint point, string textval)
        {
            var textDrawFont = new Font("Arial", 8F, FontStyle.Bold);
            var textFontSymbol = new ESRI.ArcGIS.Display.TextSymbolClass() as ESRI.ArcGIS.Display.ITextSymbol;

            textFontSymbol.Font = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIFontDispFromFont(textDrawFont) as stdole.IFontDisp;
            textFontSymbol.Font.Size = 24;
            textFontSymbol.Font.Bold = true;
            textFontSymbol.Angle = 0.0;

            String sPolygonText = textval;

            ITextElement textElement = new TextElementClass();
            textElement.Text = sPolygonText;
            textElement.Symbol = textFontSymbol;

            var element = textElement as IElement;
            element.Geometry = point;

            var pTextElementProps = element as ESRI.ArcGIS.Carto.IElementProperties3;
            pTextElementProps.Name = "OKC_Search";

            return element;
        }

        public static IElement AddGraphicLineToMap(IGeometry pnt, int green = 0, int blue = 0, int red = 255)
        {
            //Make a color to draw the polyline 
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            rgbColor.Red = red;

            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Color = rgbColor;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            simpleLineSymbol.Width = 5;

            var lineElement = new LineElementClass() { Geometry = pnt };
            lineElement.Symbol = simpleLineSymbol;
            var element = (IElement)lineElement;

            return element;
        }

        public static IElement AddGraphicPolyToMap(IGeometry pnt)
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 255;

            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Color = rgbColor;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSForwardDiagonal;
            IFillShapeElement fillShapeElement = new PolygonElementClass();
            fillShapeElement.Symbol = simpleFillSymbol;
            var element = (IElement)fillShapeElement; // Explicit Cast
            element.Geometry = pnt;

            return element;
        }


        public static string GenerateAGSToken_RESTAdmin()
        {
            try
            {
                string restAdmin = PublicModule.TokenURL;
                string loginUrl = restAdmin + "/generateToken";
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(PublicModule.AGis.AcceptAllCertifications);
                WebRequest request = WebRequest.Create(loginUrl);
                request.Method = "POST";
                string credential = "username=" + PublicModule.UserName + "&password=" + PublicModule.Password + "&client=requestip&expiration=600&f=json";
                byte[] content = Encoding.UTF8.GetBytes(credential);
                request.ContentLength = content.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(content, 0, content.Length);
                requestStream.Close();
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string result = reader.ReadToEnd();
                int index1 = result.IndexOf("token\":\"") + "token\":\"".Length;
                int index2 = result.IndexOf("\"", index1);
                //Dictionary<string, object> dictResult = DeserializeJSON(result, true);
                string token = result.Substring(index1, index2 - index1);
                return token;
            }
            catch { return ""; }
        }

        public static bool CheckWorkArea(IFeature feat)
        {
            if (PublicModule.CurrentWorkArea != null)
            {
                IPointCollection4 pointCollection4 = (IPointCollection4) feat.Shape;
                var flg = new List<bool>();
                for (var sh = 0; sh < pointCollection4.PointCount; sh++)
                {
                    IRelationalOperator2 pRel = PublicModule.CurrentWorkArea.Geometry as IRelationalOperator2;
                    flg.Add(pRel.Contains(pointCollection4.Point[sh]));
                }
                if (flg.All(a => a))
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("You are out of work bounds");
                }

                return false;
            }
            else
            {
                MessageBox.Show("You are out of work bounds");
                return false;
            }
        }
    }
}
