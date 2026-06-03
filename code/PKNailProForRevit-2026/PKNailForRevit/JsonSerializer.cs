using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    public class JsonSerializable
    {
        public virtual string ToJson()
        {
            registerFunctions();
            return Json.JsonMapper.ToJson(this);
        }

        public virtual string ToPrettyJson()
        {
            registerFunctions();
            Json.JsonWriter writer = new Json.JsonWriter();
            writer.PrettyPrint = true;
            Json.JsonMapper.ToJson(this, writer);
            return writer.ToString();
        }

        public static T FromJson<T>(string json)
        {
            registerFunctions();
            return Json.JsonMapper.ToObject<T>(json);
        }

        public static object FromJson(Type t, string json)
        {
            registerFunctions();
            return Json.JsonMapper.ToObject(t, json);
        }

        private static bool m_isFuncsRegistered = false;

        private static void registerFunctions()
        {
            if (!m_isFuncsRegistered)
            {
                // Distance
                Json.JsonMapper.RegisterExporter<Distance>(Distance.JsonExporter);
                Json.JsonMapper.RegisterImporter<double, Distance>(Distance.JsonImporter);

                // Radians
                Json.JsonMapper.RegisterExporter<Geometry2D.Radians>(Geometry2D.Radians.JsonExporter);
                Json.JsonMapper.RegisterImporter<double, Geometry2D.Radians>(Geometry2D.Radians.JsonImporter);

                // SurveyDirection
                Json.JsonMapper.RegisterExporter<SurveyDirection>(SurveyDirection.JsonExporter);
                Json.JsonMapper.RegisterImporter<int, SurveyDirection>(SurveyDirection.JsonImporter);

                // SurveyOrientation
                Json.JsonMapper.RegisterExporter<SurveyOrientation>(SurveyOrientation.JsonExporter);
                Json.JsonMapper.RegisterImporter<int, SurveyOrientation>(SurveyOrientation.JsonImporter);

                m_isFuncsRegistered = true;
            }
        }
    }
}
