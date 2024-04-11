using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        //pisanje na Analogni izlaz

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


            //sadrzaj zahteva: Function Code (1 byte), Register Address (2 byte), Register Value (2 byte)
            message[7] = CommandParameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusWriteCommandParameters)CommandParameters).OutputAddress)), 0, message, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusWriteCommandParameters)CommandParameters).Value)), 0, message, 10, 2);

            return message;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //Zaglavlja: 7 byte

            //U odgovor: Function code (1 byte), Register Address (2 byte), Register value (2 byte)

            Dictionary<Tuple<PointType, ushort>, ushort> responseValue = new Dictionary<Tuple<PointType, ushort>, ushort>();

            //Function code == Original code + 0x80 -> Error

            if (response[7] == CommandParameters.FunctionCode + 0x80)
            {
                HandeException(response[8]);
            }
            else
            {
                ushort address = BitConverter.ToUInt16(response, 8);
                ushort value = BitConverter.ToUInt16(response, 10);

                address = (ushort)IPAddress.NetworkToHostOrder((short)address);
                value = (ushort)IPAddress.NetworkToHostOrder((short)value);


                responseValue.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value);
            }

            return responseValue;
        }
    }
}