using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Models
{    
    /// <summary>
    /// This class serves two purposes; provide XML serialization for packaging and binary serialization for the clipboard.
    /// </summary>
    [Serializable]
    [XmlType("Cue")]
    public class CueModel : ISerializable
    {
        private const string IdSerializationInfoKey = "id";
        private const string StartSerializationInfoKey = "start";
        private const string LengthSerializationInfoKey = "length";
        private const string LeadInSerializationInfoKey = "leadin";
        private const string NotesSerializationInfoKey = "notes";

        public CueModel()
        {
        }

        [XmlIgnore]
        public EventCueViewModel Reference { get; private set; }

        public CueModel(EventCueViewModel viewModel)
        {
            Id = Guid.NewGuid().ToString();
            LeadIn = viewModel.LeadIn;
            Start = viewModel.Start;
            Length = viewModel.Length;
            Notes = viewModel.Notes;
            Reference = viewModel;
        }

        public CueModel(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetString(IdSerializationInfoKey);
            StartXml = info.GetString(StartSerializationInfoKey);
            LengthXml = info.GetString(LengthSerializationInfoKey);
            LeadInXml = info.GetString(LeadInSerializationInfoKey);
            Notes = info.GetString(NotesSerializationInfoKey);
        }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlIgnore]
        public TimeSpan LeadIn { get; set; }

        [XmlAttribute("LeadIn", DataType = "duration")]
        public string LeadInXml
        {
            get { return XmlConvert.ToString(LeadIn); }
            set { LeadIn = XmlConvert.ToTimeSpan(value); }
        }

        [XmlIgnore]
        public TimeSpan Start { get; set; }

        [XmlAttribute("Start", DataType = "duration")]
        public string StartXml
        {
            get { return XmlConvert.ToString(Start); }
            set { Start = XmlConvert.ToTimeSpan(value); }
        }

        [XmlIgnore]
        public TimeSpan Length { get; set; }

        [XmlAttribute("Length", DataType = "duration")]
        public string LengthXml
        {
            get { return XmlConvert.ToString(Length); }
            set { Length = XmlConvert.ToTimeSpan(value); }
        }

        [XmlAttribute]
        public string Notes { get; set; }

        public EventCueViewModel GetViewModel()
        {
            Reference = new EventCueViewModel(0, Start, Length, LeadIn)
            {
                Notes = Notes
            };

            return Reference;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(IdSerializationInfoKey, Id);
            info.AddValue(StartSerializationInfoKey, StartXml);
            info.AddValue(LengthSerializationInfoKey, LengthXml);
            info.AddValue(LeadInSerializationInfoKey, LeadInXml);
            info.AddValue(NotesSerializationInfoKey, Notes);
        }
    }
}