using System;
using System.Xml;
using System.Xml.Serialization;
using LaunchPad2.ViewModels;

namespace LaunchPad2.Models
{
    public class DeviceModel
    {
        public DeviceModel()
        {
        }

        public DeviceModel(DeviceViewModel viewModel)
        {
            Id = viewModel.Id;
            Name = viewModel.Name;
            Length = viewModel.Length;
            LeadIn = viewModel.LeadIn;
            Notes = viewModel.Notes;
        }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlIgnore]
        public TimeSpan LeadIn { get; set; }

        [XmlAttribute("LeadIn", DataType = "duration")]
        public string LeadInXml
        {
            get { return XmlConvert.ToString(LeadIn); }
            set { LeadIn = XmlConvert.ToTimeSpan(value); }
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

        public DeviceViewModel GetViewModel()
        {
            return new DeviceViewModel
            {
                Id = Id,
                Length = Length,
                LeadIn = LeadIn,
                Name = Name,
                Notes = Notes
            };
        }
    }
}