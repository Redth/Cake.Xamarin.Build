//using System;
//using System.Collections.Generic;
//using System.IO;
//using Cake.Core.IO;
//using ICSharpCode.SharpZipLib.Zip;

//namespace Cake.Xamarin.Build
//{
//	public class ZipEntryInfo
//	{
//		public long RangeStart { get; set; }
//		public string EntryName { get; set; }
//		public long CompressedSize { get; set; }

//		public long RangeEnd
//		{
//			get { return RangeStart + CompressedSize; }
//		}
//	}

//	internal static class PartialZip
//	{
//		internal static string ReadEntryText(string zipFilename, string entryName, bool readBinaryAsHex = false)
//		{
//			using (var fs = File.OpenRead(zipFilename))
//			using (var zipFile = new ZipFile(fs))
//			{
//				var entry = zipFile.GetEntry(entryName);

//				if (entry == null)
//					return null;

//				using (var entryStream = zipFile.GetInputStream(entry))
//				{
//					if (readBinaryAsHex)
//					{
//						using (var ms = new MemoryStream())
//						{
//							entryStream.CopyTo(ms);

//							var data = ms.ToArray();
//							var sb = new System.Text.StringBuilder();
//							for (int i = 0; i < data.Length; i++)
//								sb.Append(data[i].ToString("X2"));

//							return sb.ToString().ToLower();
//						}
//					}
//					else
//					{
//						string result = null;

//						using (var sr = new StreamReader(entryStream))
//							result = sr.ReadToEnd();

//						return result;
//					}
//				}
//			}
//		}

//		internal static List<ZipEntryInfo> FindZipFileRanges(string zipFilename)
//		{
//			var downloadInfo = new List<ZipEntryInfo>();

//			using (var fs = File.OpenRead(zipFilename))
//			using (var zipFile = new ZipFile(fs))
//			{
//				var entriesEnumerator = zipFile.GetEnumerator();

//				while (entriesEnumerator.MoveNext())
//				{
//					var entry = entriesEnumerator.Current as ZipEntry;

//					// DRAGONS!  Using a private method to get the location of the start of the entry in the zip file
//					var locateEntryMethod = zipFile.GetType().GetMethod("LocateEntry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//					var locateOffset = (long)locateEntryMethod.Invoke(zipFile, new object[] { entry });

//					downloadInfo.Add(new ZipEntryInfo
//					{
//						EntryName = entry.Name,
//						RangeStart = locateOffset,
//						CompressedSize = entry.CompressedSize,
//					});
//				}

//				return downloadInfo;
//			}
//		}
//	}
//}
