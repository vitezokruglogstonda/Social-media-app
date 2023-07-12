using Instakilogram.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Instakilogram.Services
{
    public class PasswordHasher
    {
        public byte[] Key { get; set; }
        
        public PasswordHasher(IUserService service, byte[]? key = null)
        {
            if(key!=null)
            {
                this.Key = new byte[key.Length];
                this.Key = key.ToArray();
            }
            else
            {
                this.Key = Encoding.UTF8.GetBytes(service.GenerateCookie(8));
            }
        }

        public byte[] ComputeHash(byte[] password)
        {
            int index = 0;
            int moduo;
            if (password.Length > this.Key.Length)
            {
                byte[] result = new byte[password.Length * 2];
                for (int i = 0; i < password.Length; i++)
                {
                    moduo = i % this.Key.Length;
                    result[index] = this.Key[moduo];
                    index++;
                    result[index] = Operation(password[i],i);
                    index++;
                }
                return result;
            }
            else
            {
                byte[] result = new byte[this.Key.Length * 2];
                for (int i = 0; i < this.Key.Length; i++)
                {
                    moduo = i % password.Length;
                    result[index] = Operation(password[moduo], i);
                    index++;
                    result[index] = this.Key[i];
                    index++;
                }
                return result;
            }
        }

        //public byte[] ComputeHash(byte[] password)
        //{
        //    int offset;
        //    if (password.Length > 4)
        //    {
        //        offset = 4;
        //    }
        //    else
        //    {
        //        offset = 5;
        //    }
        //    byte[] result = new byte[password.Length + offset];
        //    byte[] new_password = new byte[password.Length];

        //    int key_index=0;
        //    for(int i=0; i<password.Length; i++)
        //    {
        //        key_index = i % 4;
        //        if (i % 2 == 0)
        //        {
        //            //new_password[i] = Convert.ToByte(RotateLeft(BitConverter.ToInt32(new_password, i),BitConverter.ToInt32(this.Key,key_index)));
        //            new_password[i] = RotateLeft(new_password[i], this.Key[key_index]);
        //        }
        //        else
        //        {
        //            //new_password[i] = Convert.ToByte(RotateRight(BitConverter.ToInt32(new_password, i), BitConverter.ToInt32(this.Key, key_index)));
        //            new_password[i] = RotateRight(new_password[i], this.Key[key_index]);
        //        }
        //    }
        //    if (password.Length > 4)
        //    {
        //        for (int i = 0; i < key_index; i++)
        //        {

        //        }
        //    }
        //    else
        //    {
        //        this.Key.CopyTo(result, 0);
        //        new_password.CopyTo(result, 4);
        //    }

        //    this.Key.CopyTo(result, 0);
        //    new_password.CopyTo(result, 4);

        //    return result;
        //}

        private byte Operation(byte value, int iteration)
        {
            int value_int = (int)value;
            if (iteration % 2 == 0)
            {
                value_int += 2;
                return (byte)value_int;
            }
            else
            {
                value_int -= 2;
                return (byte)value_int;
            }
        }

        private byte RotateLeft(byte value, byte count)
        {
            //return (value << count) | (value >> (32 - count));
            uint count_int = (uint)count;
            uint value_int = (uint)value;
            const int byteLength = 8;

            for (int i = 0; i < count_int; i++)
            {
                uint tmp_value = value_int; // Shift operators do not work on a byte
                uint mask = 1 << byteLength - 1; //set mask to highest bit = 128
                uint extract = 0;
                uint rotated = 0;
                uint tmp_result = 0;
                for (int j = 0; i < byteLength; i++)
                {
                    extract = tmp_value & mask; // Extract one bit from byte
                    mask = mask >> 1; // Set mask to next lower bit  
                    rotated = extract << 2 * j + 1; // Move bit up
                    tmp_result += rotated; // Add to result
                    rotated = 0; // Clear
                }
                tmp_result >>= byteLength; // Move all bits back ==> rotated!
                value_int = tmp_result;
            }

            return (byte)value_int;
        }

        private byte RotateRight(byte value, byte count)
        {
            //return (value >> count) | (value << (32 - count));
            uint count_int = (uint)count;
            uint value_int = (uint)value;
            const int byteLength = 8;

            for (int i = 0; i < count_int; i++)
            {
                uint tmp_value = value_int; // Shift operators do not work on a byte
                uint mask = 1; //set mask to lowest bit = 1
                uint extract = 0;
                uint rotated = 0;
                uint tmp_result = 0;
                for (int j = 0; i < byteLength; i++)
                {
                    extract = tmp_value & mask; // Extract one bit from byte
                    mask = mask << 1; // Set mask to next higher bit  
                    rotated = extract >> 2 * j + 1; // Move bit down
                    tmp_result += rotated; // Add to result
                    rotated = 0; // Clear
                }
                tmp_result <<= byteLength; // Move all bits back ==> rotated!
                value_int = tmp_result;
            }

            return (byte)value_int;
        }

    }
}
