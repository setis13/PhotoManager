using System;
using System.Collections;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PetCommon.Contexts
{
    /// <summary>
    /// конвертирование объектов и исключений в xml
    /// </summary>
    public class XmlSerializer
	{
		#region Singleton
		private XmlSerializer()
		{
		}

		public static XmlSerializer Instance
		{
			get
			{
				if (_instance == null)
					_instance = new XmlSerializer();
				return _instance;
			}
		}

		private static XmlSerializer _instance;
		#endregion Singleton

		public SqlXml SerializeObject(Object pObject)
		{
			if (pObject is Exception)
			{
				var xmlException = ExceptionXElement((Exception) pObject, false);
				return new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(xmlException.ToString())));
			}
			else
			{
				var memoryStream = new MemoryStream();
				var xs = new System.Xml.Serialization.XmlSerializer(pObject.GetType());
				var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode);
				xs.Serialize(xmlTextWriter, pObject);
				return new SqlXml(xmlTextWriter.BaseStream);
			}
		}

		public XElement ExceptionXElement(Exception exception, bool omitStackTrace)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}
			var root = new XElement
				(exception.GetType().ToString());

			root.Add(new XElement("Message", exception.Message));

			if (!omitStackTrace && exception.StackTrace != null)
			{
				root.Add
					(
						new XElement("StackTrace",
									 from frame in exception.StackTrace.Split('\n')
									 let prettierFrame = frame.Substring(2).Trim()
									 select new XElement("Frame", prettierFrame))
					);
			}

			if (exception.Data.Count > 0)
			{
				root.Add
					(
						new XElement("Data",
									 from entry in
										 exception.Data.Cast<DictionaryEntry>()
									 let key = entry.Key.ToString()
									 let value = (entry.Value == null)
													? "null"
													: entry.Value.ToString()
									 select new XElement(key, value))
					);
			}

			if (exception.InnerException != null)
				root.Add(ExceptionXElement(exception.InnerException, omitStackTrace));

			return root;
		}
	}
}
