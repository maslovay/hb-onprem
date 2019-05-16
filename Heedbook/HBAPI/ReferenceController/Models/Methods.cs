using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace ReferenceController.Models
{
    public static class Methods
    {
        public static string MakeExpiryHash(DateTime expiry)
        {
            const string salt = "Secret Phrase";                                                                    
            string str1 = "";                                                                                       
            byte[] bytes = Encoding.UTF8.GetBytes(salt + expiry.ToString("s"));                                     
            using (var sha = System.Security.Cryptography.SHA1.Create())                                            
            {
                IEnumerable<string> listString = sha.ComputeHash(bytes).Select(b => b.ToString("x2"));              
                str1 = string.Concat(listString).Substring(8);                                                      
            }
            Console.WriteLine(str1);
            return str1;                                                                                            
        }
        public static Stream convertMemoryStreamToStream(MemoryStream MS)
        {
            MemoryStream S = new MemoryStream();
            byte[] buffer = new byte[32 * 1024]; // 32K buffer for example
            int bytesRead;
            while ((bytesRead = MS.Read(buffer, 0, buffer.Length)) > 0)
            {
                S.Write(buffer, 0, bytesRead);
            }
            S.Position = 0;
            return (Stream)S;
        }       
    }
}
