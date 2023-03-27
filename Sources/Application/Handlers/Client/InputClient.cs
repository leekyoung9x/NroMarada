using System;
using System.Collections.Generic;
using NRO_Server.Main.Menu;
using NRO_Server.Application.Constants;
using NRO_Server.Application.IO;
using NRO_Server.Application.Main;
using NRO_Server.Application.Threading;
using NRO_Server.Application.Manager;
using NRO_Server.DatabaseManager;
using NRO_Server.DatabaseManager.Player;
using NRO_Server.Model.Template;
using NRO_Server.Model.Option;
using NRO_Server.Model.Character;
using NRO_Server.Application.Map;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Reflection.Metadata;
using NRO_Server.Game;
using NRO_Server.Application.Interfaces.Character;

namespace NRO_Server.Application.Handlers.Client
{
    public static class InputClient
    {
     
        public static void HanleInputClient(Model.Character.Character character, Message message)
        {
            if(message == null) return;
            try
            {
                var lengthInput = message.Reader.ReadByte();
                var listInput = new List<string>();
                for (var i = 0; i < lengthInput; i++)
                {
                    listInput.Add(message.Reader.ReadUTF());
                }
                if(listInput.Count <= 0) return;
                switch (character.TypeInput)
                {
                    case 0://Nạp thẻ
                        {
                            var soSeriText = listInput[0];
                            var maPinText = listInput[1];

                            Console.WriteLine("Loai the " + character.NapTheTemp.LoaiThe + " menh gia " + character.NapTheTemp.MenhGia + " So Seri " + soSeriText + " ma pin " + maPinText);
                            GachThe.SendCard(character, character.NapTheTemp.LoaiThe, character.NapTheTemp.MenhGia, soSeriText, maPinText);
                            break;
                        }
                    case 1://Gift code 
                        {
                            var codeInput = listInput[0];
                            Giftcode.HandleUseGiftcode(character, listInput[0]);
                            break;
                        }
                    case 2://đổi mật khẩu
                        {
                            var timeServer = ServerUtils.CurrentTimeMillis();
                            character.Delay.UseGiftCode = timeServer + 30000;
                            var oldPass = listInput[0];
                            var newPass = listInput[1];
                            // var sdt = listInput[2];
                            var checkData = UserDB.CheckBeforeChangePass(character.Player.Id, oldPass);
                            if (!checkData)
                            {
                                character.CharacterHandler.SendMessage(Service.OpenUiSay((short)character.ShopId, "Thông tin tài khoản không chính xác, vui lòng nhập lại."));
                                return;
                            }
                            UserDB.DoiMatKhau(character.Player.Id, newPass);
                            character.CharacterHandler.SendMessage(Service.OpenUiSay((short)character.ShopId, "Đổi mật khẩu thành công, vui lòng thoát game và đăng nhập lại"));
                            break;
                        }
                    case 3: //khoa tai khoan
                        {
                            var tenNhanVat = listInput[0];
                            var banReason = listInput[1];
                            var @char = (Model.Character.Character)ClientManager.Gi().GetCharacter(tenNhanVat);
                            if (@char != null)
                            {
                                UserDB.BanUser(@char.Player.Id);
                                ClientManager.Gi().SendMessageCharacter(Service.ServerChat("Nhân vật " + tenNhanVat + " đã bị khóa tài khoản với lý do: " + banReason));
                                ClientManager.Gi().KickSession(@char.Player.Session);
                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không tìm thấy tên nhân vật này đang online"));
                            }
                            Console.WriteLine("ten TK: " + tenNhanVat + " ly do ban: " + banReason);
                            break;
                        }
                    case 4:
                        var NameCanBuff = listInput[0];
                        var IdItem = listInput[1];
                        var quantity = listInput[2];
                        int id = Int32.Parse(IdItem);
                        int soluong = Int32.Parse(quantity);
                        var @nhanvat = (Model.Character.Character)ClientManager.Gi().GetCharacter(NameCanBuff);
                        if (@nhanvat != null) {
                            var itemAdd = ItemCache.GetItemDefault((short)id);
                            var template = ItemCache.ItemTemplate(itemAdd.Id);
                            if (template.IsUpToUp)
                            {
                                try
                                {
                                    soluong = id;
                                    if (soluong <= 0) soluong = 1;
                                    if (soluong > 99) soluong = 99;
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }

                            itemAdd.Options.Add(new OptionItem()
                            {
                                Id = 30,
                                Param = 0,
                            });


                            itemAdd.Quantity = soluong;

                            @nhanvat.CharacterHandler.AddItemToBag(true, itemAdd, "Admin");
                            @nhanvat.CharacterHandler.SendMessage(Service.SendBag(character));
                            @nhanvat.CharacterHandler.SendMessage(
                                Service.ServerMessage(string.Format(TextServer.gI().ADD_ITEM,
                                    $"x{soluong} {template.Name}")));
                            break;
                        } else
                        {
                            character.CharacterHandler.SendMessage(Service.DialogMessage("Không tìm thấy tên nhân vật này đang online"));
                        }
                        break;
                    case 5:
                        var idBoss = Int32.Parse(listInput[0]);
                        var superBroly = new Boss();
                        superBroly.CreateBoss(idBoss, character.InfoChar.X, character.InfoChar.Y);
                        superBroly.CharacterHandler.SetUpInfo();
                        character.Zone.ZoneHandler.AddBoss(superBroly);
                        break;
                    case 6:
                        var sotenciuoc = Int32.Parse(listInput[0]);
                        if (sotenciuoc < 500000000)
                        {
                            if (character.InfoChar.Gold < sotenciuoc || sotenciuoc % 10 != 0)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotenciuoc <= character.InfoChar.Gold)
                            {
                                character.MineGold(sotenciuoc);
                                character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Tài Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                 

                                }
                                int x = ServerUtils.RandomNumber(1, 6);
                                int y = ServerUtils.RandomNumber(1, 6);
                                int z = ServerUtils.RandomNumber(1, 6);

                                if (4 <= (x + y + z) && (x + y + z) <= 10)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x.ToString() + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotenciuoc + " Vàng vào Tài"
                                                    + "\nRa : Xỉu"
                                                    + "\nCòn cái nịt."));
                                   
                                    return;
                                }
                                else if (x == y && x == z)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotenciuoc + " Vàng vào Tài"
                                                    + "\nRa : Tam hoa"
                                                    + "\nCòn cái nịt."));
                                   
                                    return;


                                }
                                else if ((x + y + z) > 10)
                                {
                                    character.PlusGold((sotenciuoc * 2) - (sotenciuoc / 10));
                                    character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotenciuoc + " Vàng vào Tài"
                                                    + "\nRa : Tài"
                                                     + "\nBạn nhận được :"+((sotenciuoc * 2) - (sotenciuoc / 10)) + "Vàng"
                                                    + "\nVề bờ"));
                                   
                                    SoiCautx.SoiCautxe(character.Name, "Kết quả Tai (" + (x + y + z ) + ") ăn " + ((sotenciuoc * 2) - (sotenciuoc / 10)) + "Vàng", DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));
                                    return;
                                }


                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 500tr"));
                        break;
                    case 7:
                        var cuocxiu = Int32.Parse(listInput[0]);

                        if (cuocxiu < 500000000) {
                            if (character.InfoChar.Gold < cuocxiu || cuocxiu % 10 != 0)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (cuocxiu <= character.InfoChar.Gold)
                            {
                                character.MineGold(cuocxiu);
                                character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Xỉu Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 6);
                                int y = ServerUtils.RandomNumber(1, 6);
                                int z = ServerUtils.RandomNumber(1, 6);
                                if (4 > (x + y + z) || (x + y + z) > 10)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + cuocxiu + " Vàng vào Xỉu"
                                                    + "\nRa : Tài"
                                                    + "\nCòn cái nịt."));
                               

                                    return;
                                }
                                else if (x == y && x == z)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + cuocxiu + " Vàng vào Xỉu"
                                                    + "\nRa : Tam hoa"
                                                    + "\nCòn cái nịt."));
                                    
                                    return;


                                }
                                else if ((x + y + z) <= 10 && (x + y + z) >= 4)
                                {
                                   character.PlusGold((cuocxiu * 2) - (cuocxiu / 10));
                                    character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + cuocxiu + " Vàng vào Xỉu"
                                                    + "\nRa : Xỉu"
                                                     + "\nBạn nhận được :" +((cuocxiu * 2) - (cuocxiu / 10)) + "Vàng"
                                                    + "\nVề bờ"));
                                   
                                    SoiCautx.SoiCautxe(character.Name, "Kết quả Xiur (" + (x + y + z) + ") ăn " + ((cuocxiu * 2) - (cuocxiu / 10)) + "Vàng", DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));

                                    return;
                                }
                               

                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 500tr"));
                        break;
                    case 8:
                        var cuocchan = Int32.Parse(listInput[0]);
                        if (    cuocchan < 500000000)
                        {
                            if (character.InfoChar.Gold < cuocchan || cuocchan % 10 != 0)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (cuocchan <= character.InfoChar.Gold)
                            {
                                character.MineGold(cuocchan);
                                character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                               
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Chẵn Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 9);
                                int y = ServerUtils.RandomNumber(1, 9);
                                int z = ServerUtils.RandomNumber(1, 9);
                                int t = ServerUtils.RandomNumber(1, 9);

                                if ((x + y + z + t) % 2 != 0)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z+t)
                                                    + "\nBạn đã cược : " + cuocchan + " Vàng vào Chẵn"
                                                    + "\nRa : Lẻ"
                                                    + "\nCòn cái nịt."));

                                    return;
                                }
                               
                                else if ((x + y + z + t) % 2 == 0)
                                {
                                    character.PlusGold((cuocchan * 2) - (cuocchan / 10));
                                    character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z+t)
                                                    + "\nBạn đã cược : " + cuocchan + " Vàng vào Chẵn"
                                                    + "\nRa : Chẵn"
                                                     + "\nBạn nhận được :" + ((cuocchan * 2) - (cuocchan / 10)) + "Vàng"
                                                    + "\nVề bờ"));

                                    return;
                                }


                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 500tr"));
                        break;
                    case 9:
                        var sotenciuocle = Int32.Parse(listInput[0]);
                        if (sotenciuocle < 500000000)
                        {
                            if (character.InfoChar.Gold < sotenciuocle || sotenciuocle % 10 != 0)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotenciuocle <= character.InfoChar.Gold)
                            {
                                character.MineGold(sotenciuocle);
                                character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
          
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Lẻ Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 9);
                                int y = ServerUtils.RandomNumber(1, 9);
                                int z = ServerUtils.RandomNumber(1, 9);
                                int t = ServerUtils.RandomNumber(1, 9);

                                if ((x + y + z + t) % 2== 0)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z+t).ToString()
                                                    + "\nBạn đã cược : " + sotenciuocle.ToString() + " Vàng vào Lẻ"
                                                    + "\nRa : Chẵn"
                                                    + "\nCòn cái nịt."));

                                    return;
                                }

                                else if ((x + y + z + t) % 2 != 0)
                                {
                                    character.PlusGold((sotenciuocle * 2) - (sotenciuocle / 10));
                                    character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z+t)
                                                    + "\nBạn đã cược : " + sotenciuocle + " Vàng vào Lẻ"
                                                    + "\nRa : Lẻ"
                                                     + "\nBạn nhận được :" +( (sotenciuocle * 2) - (sotenciuocle / 10)) + "Vàng"
                                                    + "\nVề bờ"));

                                    return;
                                }

                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 500tr"));

                        break;
                    case 10://vip
                        var sotvtai = Int32.Parse(listInput[0]);
                        var bagNull = character.LengthBagNull();
                        var thoivang = ItemCache.GetItemDefault(457);
                        if (sotvtai < 1000 && character.CharacterHandler.GetItemBagById(457) != null )
                        {
                            if (character.CharacterHandler.GetItemBagById(457).Quantity < sotvtai || sotvtai % 10 != 0||character.CharacterHandler.GetItemBagById(457) == null )
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Thỏi Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotvtai <= character.CharacterHandler.GetItemBagById(457).Quantity)
                            {
                                character.CharacterHandler.RemoveItemBagById(457, sotvtai, reason: "CLTX");
                                character.CharacterHandler.SendMessage(Service.SendBag(character));
                                
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Tài Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);


                                }
                                int x = ServerUtils.RandomNumber(1, 6);
                                int y = ServerUtils.RandomNumber(1, 6);
                                int z = ServerUtils.RandomNumber(1, 6);

                                if (4 <= (x + y + z) && (x + y + z) <= 10)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x.ToString() + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvtai + " Thỏi Vàng vào Tài"
                                                    + "\nRa : Xỉu"
                                                    + "\nCòn cái nịt."));
                                  
                                    return;
                                }
                                else if (x == y && x == z)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvtai + " Thỏi Vàng vào Tài"
                                                    + "\nRa : Tam hoa"
                                                    + "\nCòn cái nịt."));
                                   
                                    return;


                                }
                                else if ((x + y + z) > 10)
                                {
                                    if (bagNull < 3)
                                    {
                                        character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().NOT_ENOUGH_BAG));
                                        return;
                                    }
                                    thoivang.Quantity = (sotvtai * 2) - (sotvtai / 10);
                                    character.CharacterHandler.AddItemToBag(true, thoivang, "CLTX");
                                    character.CharacterHandler.SendMessage(Service.SendBag(character));
                                    character.CharacterHandler.SendMessage(Service.ServerMessage(string.Format(TextServer.gI().GET_GOLD_BAR, (sotvtai * 2) - (sotvtai / 10))));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvtai + " Thỏi Vàng vào Tài"
                                                    + "\nRa : Tài"
                                                     + "\nBạn nhận được :" + ((sotvtai * 2) - (sotvtai / 10)) + "Thỏi Vàng"
                                                    + "\nVề bờ"));
                                    
                                    SoiCautx.SoiCautxe(character.Name, "Kết quả Tai (" + (x + y + z) + ") ăn " + ((sotvtai * 2) - (sotvtai / 10)) + "Vàng", DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));
                                    return;
                                }


                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Thỏi Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 1000tv"));
                        break;
                    case 11:
                        var sotvxiu = Int32.Parse(listInput[0]);
                        var thoivang1 = ItemCache.GetItemDefault(457);
                        var bagNull1 = character.LengthBagNull();
                        if (sotvxiu < 1000 && character.CharacterHandler.GetItemBagById(457) != null)
                        {
                            if (character.CharacterHandler.GetItemBagById(457).Quantity < sotvxiu || sotvxiu % 10 != 0 || character.CharacterHandler.GetItemBagById(457) == null)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Thỏi Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotvxiu <= character.CharacterHandler.GetItemBagById(457).Quantity)
                            {
                                character.CharacterHandler.RemoveItemBagById(457, sotvxiu, reason: "CLTX");
                                character.CharacterHandler.SendMessage(Service.SendBag(character));
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Xỉu Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 6);
                                int y = ServerUtils.RandomNumber(1, 6);
                                int z = ServerUtils.RandomNumber(1, 6);
                                if (4 > (x + y + z) || (x + y + z) > 10)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvxiu + " Thỏi Vàng vào Xỉu"
                                                    + "\nRa : Tài"
                                                    + "\nCòn cái nịt."));


                                    return;
                                }
                                else if (x == y && x == z)
                                {
                            
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvxiu + "Thỏi Vàng vào Xỉu"
                                                    + "\nRa : Tam hoa"
                                                    + "\nCòn cái nịt."));

                                    return;


                                }
                                else if ((x + y + z) <= 10 && (x + y + z) >= 4)
                                {
                                    if (bagNull1 < 3)
                                    {
                                        character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().NOT_ENOUGH_BAG));
                                        return;
                                    }
                                    thoivang1.Quantity = (sotvxiu * 2) - (sotvxiu / 10);
                                    character.CharacterHandler.AddItemToBag(true, thoivang1, "CLTX");
                                    character.CharacterHandler.SendMessage(Service.SendBag(character));
                                    character.CharacterHandler.SendMessage(Service.ServerMessage(string.Format(TextServer.gI().GET_GOLD_BAR, (sotvxiu * 2) - (sotvxiu / 10))));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z
                                                    + "\nTổng là : " + (x + y + z)
                                                    + "\nBạn đã cược : " + sotvxiu + " Thỏi Vàng vào Xỉu"
                                                    + "\nRa : Xỉu"
                                                     + "\nBạn nhận được :" + ((sotvxiu * 2) - (sotvxiu / 10)) + "Thỏi Vàng"
                                                    + "\nVề bờ"));
                                   
                                    SoiCautx.SoiCautxe(character.Name, "Kết quả Xiur (" + (x + y + z) + ") ăn " + ((sotvxiu * 2) - (sotvxiu / 10)) + "Vàng", DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));

                                    return;
                                }


                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 1000Thỏi"));
                        break;
                    case 12:
                        var sotvchan = Int32.Parse(listInput[0]);
                        var thoivang2 = ItemCache.GetItemDefault(457);
                        var bagNull2 = character.LengthBagNull();
                        if (sotvchan < 1000 && character.CharacterHandler.GetItemBagById(457) != null)
                        {
                            if (character.CharacterHandler.GetItemBagById(457).Quantity < sotvchan || sotvchan % 10 != 0 || character.CharacterHandler.GetItemBagById(457) == null)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Thỏi Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotvchan <= character.CharacterHandler.GetItemBagById(457).Quantity)
                            {
                                character.CharacterHandler.RemoveItemBagById(457, sotvchan, reason: "CLTX");
                                character.CharacterHandler.SendMessage(Service.SendBag(character));
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Chẵn Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 9);
                                int y = ServerUtils.RandomNumber(1, 9);
                                int z = ServerUtils.RandomNumber(1, 9);
                                int t = ServerUtils.RandomNumber(1, 9);

                                if ((x + y + z + t) % 2 != 0)
                                {
                                  
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z + t)
                                                    + "\nBạn đã cược : " + sotvchan + " Thỏi Vàng vào Chẵn"
                                                    + "\nRa : Lẻ"
                                                    + "\nCòn cái nịt."));

                                    return;
                                }

                                else if ((x + y + z + t) % 2 == 0)
                                {

                                    if (bagNull2 < 3)
                                    {
                                        character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().NOT_ENOUGH_BAG));
                                        return;
                                    }
                                    thoivang2.Quantity = (sotvchan* 2) - (sotvchan / 10);
                                    character.CharacterHandler.AddItemToBag(true, thoivang2, "CLTX");
                                    character.CharacterHandler.SendMessage(Service.SendBag(character));
                                    character.CharacterHandler.SendMessage(Service.ServerMessage(string.Format(TextServer.gI().GET_GOLD_BAR, (sotvchan * 2) - (sotvchan / 10))));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z + t)
                                                    + "\nBạn đã cược : " + sotvchan + " Thỏi Vàng vào Chẵn"
                                                    + "\nRa : Chẵn"
                                                    + "\nBạn nhận được :" + ((sotvchan * 2) - (sotvchan / 10)) + "Thỏi Vàng"
                                                    + "\nVề bờ"));

                                    return;
                                }


                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 1000Thỏi"));
                        break;
                    case 13:
                        var sotvcle = Int32.Parse(listInput[0]);
                        var thoivang3 = ItemCache.GetItemDefault(457);
                        var bagNull3 = character.LengthBagNull();
                        if (sotvcle < 1000 && character.CharacterHandler.GetItemBagById(457) != null)
                        {
                            if (character.CharacterHandler.GetItemBagById(457).Quantity < sotvcle || sotvcle % 10 != 0 || character.CharacterHandler.GetItemBagById(457) == null)
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Thỏi Vàng\nHoặc Nhập sai giá trị"));
                                return;
                            }
                            if (sotvcle <= character.CharacterHandler.GetItemBagById(457).Quantity)
                            {
                                character.CharacterHandler.RemoveItemBagById(457, sotvcle, reason: "CLTX");
                                character.CharacterHandler.SendMessage(Service.SendBag(character));
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt Lẻ Thành Công"));
                                character.CharacterHandler.SendMessage(Service.ServerMessage("Trả kết quả sau 10 s"));
                                int TimeSeconds = 10;
                                while (TimeSeconds > 0)
                                {
                                    TimeSeconds--;
                                    Thread.Sleep(1000);
                                }
                                int x = ServerUtils.RandomNumber(1, 9);
                                int y = ServerUtils.RandomNumber(1, 9);
                                int z = ServerUtils.RandomNumber(1, 9);
                                int t = ServerUtils.RandomNumber(1, 9);

                                if ((x + y + z + t) % 2 == 0)
                                {
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z + t).ToString()
                                                    + "\nBạn đã cược : " + sotvcle.ToString() + " Thỏi Vàng vào Lẻ"
                                                    + "\nRa : Chẵn"
                                                    + "\nCòn cái nịt."));

                                    return;
                                }

                                else if ((x + y + z + t) % 2 != 0)
                                {
                                    if (bagNull3 < 3)
                                    {
                                        character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().NOT_ENOUGH_BAG));
                                        return;
                                    }
                                    thoivang3.Quantity = (sotvcle * 2) - (sotvcle/ 10);
                                    character.CharacterHandler.AddItemToBag(true, thoivang3, "CLTX");
                                    character.CharacterHandler.SendMessage(Service.SendBag(character));
                                    character.CharacterHandler.SendMessage(Service.ServerMessage(string.Format(TextServer.gI().GET_GOLD_BAR, (sotvcle * 2) - (sotvcle / 10))));
                                    character.CharacterHandler.SendMessage(Service.DialogMessage("Kết Quả là : " + x + " " + y + " " + z + " " + t
                                                    + "\nTổng là : " + (x + y + z + t)
                                                    + "\nBạn đã cược : " + sotvcle + " Thỏi Vàng vào Lẻ"
                                                    + "\nRa : Lẻ"
                                                     + "\nBạn nhận được :" + ((sotvcle * 2) - (sotvcle / 10)) + "Thỏi Vàng"
                                                    + "\nVề bờ"));

                                    return;
                                }

                            }
                            else
                            {
                                character.CharacterHandler.SendMessage(Service.DialogMessage("Không đủ Vàng\nHoặc Nhập sai giá trị"));
                            }
                            return;
                        }
                        character.CharacterHandler.SendMessage(Service.DialogMessage("Đặt bé Hơn 1000Thỏi"));

                        break;
                    case 1999: //đổi vnd sang vàng
                    {
                        Console.WriteLine(listInput[0]);
                        // kiểm tra có phải là số không
                        int n;
                        bool isNumeric = int.TryParse(listInput[0], out n);
                        if (!isNumeric) 
                        {
                            character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().INPUT_CORRECT_NUMBER));
                            return;
                        }
                        var inputValue = Int32.Parse(listInput[0]);

                        if (inputValue < 0)
                        {
                            character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().INPUT_CORRECT_NUMBER));
                            return;
                        }
                        // Kiểm tra có đủ VNĐ không
                        int vnd = UserDB.GetVND(character.Player);
                        if (vnd < inputValue)
                        {
                            character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().NOT_ENOUGH_VND));
                            return;
                        }
                        // Kiểm tra giới hạn vàng trên người
                        long quyDoi = inputValue*550;
                        if (character.InfoChar.Gold + quyDoi > character.InfoChar.LimitGold)
                        {
                            var quyDoiToiDa = (character.InfoChar.LimitGold - character.InfoChar.Gold)/550;
                            character.CharacterHandler.SendMessage(Service.ServerMessage(string.Format(TextServer.gI().VND_TO_GOLD_LIMIT, ServerUtils.GetMoneys(quyDoiToiDa))));
                            return;
                        }
                        // Oke hết thì trừ VNĐ và cộng vàng
                        if (UserDB.MineVND(character.Player, inputValue))
                        {
                            character.PlusGold(quyDoi);
                            character.CharacterHandler.SendMessage(Service.MeLoadInfo(character));

                            if (inputValue >= 20000 && !character.InfoChar.IsPremium)
                            {
                                character.InfoChar.IsPremium = true;
                                character.CharacterHandler.SendMessage(Service.ServerMessage(TextServer.gI().UPGRADE_TO_PREMIUM));
                            }
                        }
                        character.TypeInput = 0;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Server.Gi().Logger.Error($"Error HanleInputClient in Service.cs: {e.Message} \n {e.StackTrace}", e);
            }
            finally
            {
                message?.CleanUp();
            }
        }

        public static void HandleNapThe(Model.Character.Character character, Message message)
        {
            var gender = character.InfoChar.Gender;
            character.CharacterHandler.SendMessage(Service.OpenUiSay(5, string.Format("Hãy đến gặp {0} để nạp thẻ bạn nhé.", TextTask.NameNpc[gender]), false, gender));
        }
    }
}