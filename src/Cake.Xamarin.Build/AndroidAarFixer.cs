using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Cake.Xamarin.Build
{
	public class AndroidAarFixer
	{
		public AndroidAarFixer()
		{
		}

		static string HashSha1 (string value)
		{
			using (HashAlgorithm hashAlg = new SHA1Managed ()) {
				var hash = hashAlg.ComputeHash (System.Text.Encoding.ASCII.GetBytes (value));
				return BitConverter.ToString (hash).Replace ("-", string.Empty);
			}
		}

		public static void FixAarFile (string aarFile, string artifactId, bool fixManifestPackageNames, bool extractProguardConfigs)
		{
			if (File.Exists(aarFile + ".tmp"))
				File.Delete(aarFile + ".tmp");
			File.Move(aarFile, aarFile + ".tmp");
			File.Delete(aarFile);

			var aarFileInfo = new FileInfo(aarFile);

			//using (var memoryStream = new MemoryStream(File.ReadAllBytes(aarFile + ".tmp")))
			using (var zipArchive = new ZipArchive(File.OpenRead(aarFile + ".tmp"), ZipArchiveMode.Read, false))
			//using (var newMemoryStream = new MemoryStream())
			using (var newZipArchive = new ZipArchive(File.Create(aarFile), ZipArchiveMode.Create, false))
			{
				var entryNames = zipArchive.Entries.Select(zae => zae.FullName).ToList();

				foreach (var entryName in entryNames)
				{
					var newName = entryName;

					// Open the old entry
					var oldEntry = zipArchive.GetEntry(entryName);

					// We are only re-adding non empty folders, otherwise we end up with a corrupt zip in mono
					if (!string.IsNullOrEmpty(oldEntry.Name))
					{

						// SPOILER ALERT: UGLY WORKAROUND
						// In the Android Support libraries, there exist multiple .aar files which have a `libs/internal_impl-25.0.0` file.
						// In Xamarin.Android, there is a Task "CheckDuplicateJavaLibraries" which inspects jar files being pulled in from .aar files
						// in assemblies to see if there exist any files with the same name but different content, and will throw an error if it finds any.
						// However, for us, it is perfectly valid to have this scenario and we should not see an error.
						// We are working around this by detecting files named like this, and renaming them to some unique value
						// in this case, a part of the hash of the assembly name.
						var newFile = Path.GetFileName(newName);
						var newDir = Path.GetDirectoryName(newName);

						if (newFile.StartsWith("internal_impl", StringComparison.InvariantCulture))
							newName = Path.Combine(newDir, "internal_impl-" + HashSha1(artifactId).Substring(0, 8) + ".jar");

						// Create a new entry based on our new name
						var newEntry = newZipArchive.CreateEntry(newName);

						// Since Xamarin.Android's AndoridManifest.xml merging code is not as sophisticated as gradle's yet, we may need
						// to fix some things up in the manifest file to get it to merge properly into our applications
						// Here we will check to see if Fixing manifests was enabled, and if the entry we are on is the AndroidManifest.xml file
						if (fixManifestPackageNames && oldEntry.Length > 0 && newName.EndsWith("AndroidManifest.xml", StringComparison.OrdinalIgnoreCase))
						{
							// android: namespace
							XNamespace xns = "http://schemas.android.com/apk/res/android";

							using (var oldStream = oldEntry.Open())
							using (var xmlReader = System.Xml.XmlReader.Create(oldStream))
							{
								var xdoc = XDocument.Load(xmlReader);

								// BEGIN FIXUP #1
								// Some `android:name` attributes will start with a . indicating, that the `package` value of the `manifest` element
								// should be prefixed dynamically/at merge to this attribute value.  Xamarin.Android doesn't handle this case yet
								// so we are going to manually take care of it.

								// Get the package name from the manifest node
								var packageName = xdoc.Document.Descendants("manifest")?.FirstOrDefault()?.Attribute("package")?.Value;

								if (!string.IsNullOrEmpty(packageName))
								{
									// Find all elements in the xml document that have a `android:name` attribute which starts with a .
									// Select all of them, and then change the `android:name` attribute value to be the
									// package name we found in the `manifest` element previously + the original attribute value
									var elemToFix = xdoc.Document.Descendants()
													.Where(elem => elem.Attribute(xns + "name")?.Value?.StartsWith(".", StringComparison.Ordinal) ?? false)
													.ToList();

									foreach (var el in elemToFix) {
										var av = packageName + el.Attribute(xns + "name").Value;
										el.SetAttributeValue(xns + "name", av);
									}
										
								}
								// END FIXUP #1

								using (var newStream = newEntry.Open())
								using (var xmlWriter = System.Xml.XmlWriter.Create(newStream))
								{
									xdoc.Save(xmlWriter);
								}
							}

							// Xamarin.Android does not consider using proguard config files from within .aar files, so we need to extract it
							// to a known location and output it to 
						}
						else if (extractProguardConfigs && oldEntry.Length > 0 && (newName.EndsWith("proguard.txt", StringComparison.OrdinalIgnoreCase) || newName.EndsWith("proguard.cfg", StringComparison.OrdinalIgnoreCase)))
						{
							// We still want to copy the file over into the .aar
							using (var oldStream = oldEntry.Open())
							using (var newStream = newEntry.Open())
							{
								oldStream.CopyTo(newStream);
							}

							// Calculate an output path beside the merged/output assembly name's md5 hash
							var proguardCfgOutputPath = Path.Combine(aarFileInfo.DirectoryName, artifactId + ".proguard.txt");

							// Create a copy of the file
							using (var oldStream = oldEntry.Open())
							using (var proguardStream = File.OpenWrite(proguardCfgOutputPath))
							{
								oldStream.CopyTo(proguardStream);
							}
						}
						else
						{
							// Copy file contents over if they exist
							if (oldEntry.Length > 0)
							{
								using (var oldStream = oldEntry.Open())
								using (var newStream = newEntry.Open())
								{
									oldStream.CopyTo(newStream);
								}
							}
						}
					}

					// Delete the old entry regardless of if it's a folder or not
					//oldEntry.Delete();
				}
			}
		}
	}
}
