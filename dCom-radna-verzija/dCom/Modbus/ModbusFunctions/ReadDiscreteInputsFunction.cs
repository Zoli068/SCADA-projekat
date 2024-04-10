using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read discrete inputs functions/requests.
    /// </summary>
    public class ReadDiscreteInputsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadDiscreteInputsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadDiscreteInputsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        //Zahtev za citanje digitalnog ulaza

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //duzina zahteva je uvek 12byte
            byte[] message = new byte[12];

            //zaglavlja zahteva: Transaction Id (2 byte), Protocol Id (2 byte), Length (2 byte), Unit Id (1 Byte)

            //short vrednost -> moramo prebaciti u Network Order
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId)), 0, message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId)), 0, message, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.Length)), 0, message, 4, 2);

            //vrednosti koji su samo 1 byte, samo prekopiramo
            message[6] = CommandParameters.UnitId;


            //sadrzaj zahteva: Function Id (1 byte), Start Address (1 byte), Quantity (2 byte)
            message[7] = CommandParameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusReadCommandParameters)CommandParameters).StartAddress)), 0, message, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusReadCommandParameters)CommandParameters).Quantity)), 0, message, 10, 2);

            return message;
        }

        //Odgovor na zahtev

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> responseValues = new Dictionary<Tuple<PointType, ushort>, ushort>();


            //Zaglavlja : 7 Byte
            //Sadrzaj odgovora: Function code (1 byte), Byte Count (1 byte), Data (N Bytes)

            //Ako imamo gresku onda Function code bice: Original Function Code + 0x80
            //i u Byte Count imacemo vrsta Error-a
            if (response[7] == CommandParameters.FunctionCode + 0x80)
            {
                HandeException(response[8]);
            }
            else
            {
                int counter = 0;
                ushort adress = ((ModbusReadCommandParameters)CommandParameters).StartAddress;
                ushort value = 0;
                byte mask = 1;


                //response[8] -> ByteCount
                //idemo byte po byte
                for (int i = 0; i < response[8]; i++)
                {
                    byte temp = response[9 + i];

                    //u svakom byte-u idemo bit po bit
                    for (int j = 0; j < 8; j++)
                    {
                        value = (ushort)(temp & mask);
                        temp >>= 1;

                        //DIGITAL_INPUT jer citamo digitalan ulaz
                        responseValues.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_INPUT, adress), value);

                        counter++;
                        adress++;

                        //provera kraja, Quantity-> broj bita
                        if (counter >= ((ModbusReadCommandParameters)CommandParameters).Quantity) break;
                    }

                }
            }

            return responseValues;
        }
    }
}