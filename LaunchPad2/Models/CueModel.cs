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
        private const string StartSerializationInfoKey = "start";
        private const string LengthSerializationInfoKey = "length";
        private const string LeadInSerializationInfoKey = "leadin";
        private const string NotesSerializationInfoKey = "notes";

        public CueModel()
        {
        }

        public CueModel(EventCueViewModel viewModel)
        {
            LeadIn = viewModel.LeadIn;
            Start = viewModel.Start;
            Length = viewModel.Length;
            Notes = viewModel.Notes;
        }

        public CueModel(SerializationInfo info, StreamingContext context)
        {
            StartXml = info.GetString(StartSerializationInfoKey);
            LengthXml = info.GetString(LengthSerializationInfoKey);
            LeadInXml = info.GetString(LeadInSerializationInfoKey);
            Notes = info.GetString(NotesSerializationInfoKey);
        }

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
            return new EventCueViewModel(0, Start, Length, LeadIn)
            {
                Notes = Notes
            };
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(StartSerializationInfoKey, StartXml);
            info.AddValue(LengthSerializationInfoKey, LengthXml);
            info.AddValue(LeadInSerializationInfoKey, LeadInXml);
            info.AddValue(NotesSerializationInfoKey, Notes);
        }
    }
}