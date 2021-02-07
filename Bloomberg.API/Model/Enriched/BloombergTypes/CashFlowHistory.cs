using System;
using Bloomberglp.Blpapi;

namespace Bloomberg.API.Model.Enriched.BloombergTypes
{
    public class CashFlowHistory : IBloombergType
    {
        public int PeriodNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Coupon { get; set; }
        public decimal Interest { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal PrincipalBalance { get; set; }

        public void ReadElement(Element raw)
        {
            PeriodNumber = raw.GetElement("Period Number").GetValueAsInt32();
            PaymentDate = DateTime.Parse(raw.GetElement("Payment Date").GetValueAsString());
            Coupon = raw.GetElement("Coupon").GetValueAsString().BloombergStringToDecimal();
            Interest = raw.GetElement("Interest").GetValueAsString().BloombergStringToDecimal();
            PrincipalPaid = raw.GetElement("Principal Paid").GetValueAsString().BloombergStringToDecimal();
            PrincipalBalance = raw.GetElement("Principal Balance").GetValueAsString().BloombergStringToDecimal();
        }

        public void ReadBits(object[] rawBits)
        {
            PeriodNumber = Convert.ToInt32((decimal)rawBits[0]);
            PaymentDate = (DateTime)rawBits[1];
            Coupon = (decimal)rawBits[2];
            Interest = (decimal)rawBits[3];
            PrincipalPaid = (decimal)rawBits[4];
            PrincipalBalance = (decimal)rawBits[5];
        }
    }
}
