using System;
using System.Text;

namespace BingoAdmin.UI.Helpers
{
    public static class PixGenerator
    {
        public static string GeneratePayload(string pixKey, decimal amount, string merchantName, string merchantCity, string txId = "***")
        {
            /*
             * 00 02 01                 -> Format Indicator (01)
             * 26 33 00 14 BR.GOV.BCB.PIX 01 11 [KEY] 02 00 -> Merchant Account Info
             * 52 04 00 00              -> Merchant Category Code (0000)
             * 53 03 986                -> Transaction Currency (BRL)
             * 54 [XX] [AMOUNT]         -> Transaction Amount
             * 58 [XX] [COUNTRY]        -> Country Code (BR)
             * 59 [XX] [NAME]           -> Merchant Name
             * 60 [XX] [CITY]           -> Merchant City
             * 62 07 05 03 ***          -> Additional Data Field Template (TxID)
             * 63 04 [CRC16]            -> CRC16
             */

            var sb = new StringBuilder();

            // 00 - Payload Format Indicator
            AppendField(sb, "00", "01");

            // 26 - Merchant Account Information
            var sbInfo = new StringBuilder();
            AppendField(sbInfo, "00", "BR.GOV.BCB.PIX");
            AppendField(sbInfo, "01", pixKey);
            // Description could be added here in field 02
            AppendField(sb, "26", sbInfo.ToString());

            // 52 - Merchant Category Code
            AppendField(sb, "52", "0000");

            // 53 - Transaction Currency
            AppendField(sb, "53", "986"); // BRL

            // 54 - Transaction Amount
            var amountStr = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            AppendField(sb, "54", amountStr);

            // 58 - Country Code
            AppendField(sb, "58", "BR");

            // 59 - Merchant Name
            var name = merchantName.Length > 25 ? merchantName.Substring(0, 25) : merchantName;
            AppendField(sb, "59", name);

            // 60 - Merchant City
            var city = merchantCity.Length > 15 ? merchantCity.Substring(0, 15) : merchantCity;
            AppendField(sb, "60", city);

            // 62 - Additional Data Field Template
            var sbAdd = new StringBuilder();
            AppendField(sbAdd, "05", txId); // Reference ID
            AppendField(sb, "62", sbAdd.ToString());

            // 63 - CRC16
            var payloadWithoutCrc = sb.ToString() + "6304";
            var crc = CRC16CCITT.Compute(payloadWithoutCrc);
            
            return payloadWithoutCrc + crc.ToString("X4");
        }

        private static void AppendField(StringBuilder sb, string id, string value)
        {
            sb.Append(id);
            sb.Append(value.Length.ToString("D2"));
            sb.Append(value);
        }
    }

    public static class CRC16CCITT
    {
        public static ushort Compute(string data)
        {
            ushort crc = 0xFFFF;
            var bytes = Encoding.ASCII.GetBytes(data);

            foreach (var b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) > 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }
    }
}
