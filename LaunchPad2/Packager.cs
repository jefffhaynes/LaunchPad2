﻿using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Mime;
using System.Xml.Serialization;
using LaunchPad2.Models;

namespace LaunchPad2
{
    public static class Packager
    {
        private const string DocumentUriPath = "Content\\Document.xml";
        static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Model));
        private static readonly object PackLock = new object();

        public static void Pack(string filename, Model model, string audioFile)
        {
            lock (PackLock)
            {
                Uri partUriDocument =
                    PackUriHelper.CreatePartUri(
                        new Uri(DocumentUriPath, UriKind.Relative));

                using (var package = Package.Open(filename, FileMode.Create))
                {
                    PackagePart packagePartDocument =
                        package.CreatePart(partUriDocument,
                            MediaTypeNames.Text.Xml);

                    Serializer.Serialize(packagePartDocument.GetStream(FileMode.Create, FileAccess.Write), model);

                    if (audioFile != null)
                    {
                        var audioFileName = Path.GetFileName(audioFile);
                        var audioResource = $"Resources\\{audioFileName}";
                        Uri partUriResource = PackUriHelper.CreatePartUri(
                            new Uri(audioResource, UriKind.Relative));

                        PackagePart packagePartResource =
                            package.CreatePart(partUriResource,
                                MediaTypeNames.Application.Octet);

                        packagePartDocument.CreateRelationship(packagePartResource.Uri,
                            TargetMode.Internal,
                            "http://schemas.openxmlformats.org/package/2006/relationships/meta data/core-properties");

                        using (var audioStream = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                            audioStream.CopyTo(packagePartResource.GetStream());
                    }
                }
            }
        }

        public static void Update(string filename, Model model)
        {
            Uri partUriDocument =
                PackUriHelper.CreatePartUri(
                    new Uri(DocumentUriPath, UriKind.Relative));

            using (var package = Package.Open(filename, FileMode.Open))
            {
                PackagePart packagePartDocument = package.GetPart(partUriDocument);
                Serializer.Serialize(packagePartDocument.GetStream(FileMode.Create, FileAccess.Write), model);
            }
        }

        public static Model Unpack(string filename, out TemporaryFile temporaryAudioFile)
        {
            using (var package = Package.Open(filename, FileMode.Open))
            {
                Uri partUriDocument =
                    PackUriHelper.CreatePartUri(
                        new Uri(DocumentUriPath, UriKind.Relative));

                var documentPart = package.GetPart(partUriDocument);

                var model = (Model) Serializer.Deserialize(documentPart.GetStream());

                var relationships = documentPart.GetRelationships();

                var audioRelationship = relationships.FirstOrDefault();

                if (audioRelationship != null)
                {
                    var audioPartUri = audioRelationship.TargetUri;
                    var audioPart = package.GetPart(audioPartUri);
                    temporaryAudioFile = new TemporaryFile();

                    using (var stream = new FileStream(temporaryAudioFile.Path, FileMode.Create, FileAccess.Write))
                        audioPart.GetStream().CopyTo(stream);
                }
                else temporaryAudioFile = null;

                return model;
            }
        }
    }
}
