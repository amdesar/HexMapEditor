using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
namespace HexMapEditor
{
    internal static class StarterSets
    {
        static List<LibraryIcon> icons;static List<ObjectTemplate> objects;
        internal static List<LibraryIcon> Icons(){if(icons==null)icons=BuildIcons();return icons.Select(i=>i.Copy()).ToList();}
        internal static List<ObjectTemplate> Objects(){if(objects==null)objects=BuildObjects();return objects.Select(o=>o.Copy()).ToList();}
        static List<LibraryIcon> BuildIcons()
        {
            string[] names={"Osada","Miasto","Zamek","Wieża","Ruiny","Jaskinia","Kopalnia","Obóz","Port","Most","Świątynia","Niebezpieczeństwo","Skarb","Pole bitwy","Cmentarz","Punkt orientacyjny","Portal","Lotnisko","Stacja kosmiczna","Anomalia"};
            return names.Select((n,i)=>new LibraryIcon{Id="starter-icon090-"+i,Name=n,Asset=Draw(n,i,false)}).ToList();
        }
        static List<ObjectTemplate> BuildObjects()
        {
            string[] names={"Postać niebieska","Postać czerwona","Postać zielona","Flaga","Koń","Wilk","Smok","Powóz","Samochód","Statek wodny","Statek kosmiczny","Skrzynia"};
            return names.Select((n,i)=>new ObjectTemplate{Id="starter-object090-"+i,Name=n,Asset=Draw(n,i,true)}).ToList();
        }
        static IconAsset Draw(string name,int index,bool obj)
        {
            using(var bitmap=new Bitmap(128,128))using(var g=Graphics.FromImage(bitmap))using(var p=new Pen(Color.WhiteSmoke,6)){g.SmoothingMode=SmoothingMode.AntiAlias;p.LineJoin=LineJoin.Round;p.StartCap=p.EndCap=LineCap.Round;
                if(obj){
                    if(index<3){using(var b=new SolidBrush(new[]{Color.FromArgb(85,155,235),Color.FromArgb(228,95,95),Color.FromArgb(102,189,119)}[index])){g.FillEllipse(b,45,17,38,38);g.FillPolygon(b,new[]{new Point(45,59),new Point(83,59),new Point(97,108),new Point(31,108)});}g.DrawEllipse(p,45,17,38,38);g.DrawLine(p,49,78,43,110);g.DrawLine(p,79,78,85,110);}
                    else if(index==3){g.DrawLine(p,35,18,35,111);g.FillPolygon(Brushes.WhiteSmoke,new[]{new Point(36,20),new Point(99,20),new Point(82,44),new Point(99,67),new Point(36,67)});}
                    else if(index>=4&&index<=6){g.DrawEllipse(p,27,48,65,35);g.DrawLines(p,new[]{new Point(75,56),new Point(84,23),new Point(108,39),new Point(94,59)});g.DrawLine(p,38,78,29,108);g.DrawLine(p,78,79,90,108);g.DrawLine(p,27,55,15,40);if(index==5){g.DrawLine(p,83,27,76,15);g.DrawLine(p,97,30,105,16);}if(index==6)g.DrawPolygon(p,new[]{new Point(58,54),new Point(27,15),new Point(22,55),new Point(64,78)});}
                    else if(index==7||index==8){g.DrawRectangle(p,21,42,85,43);g.DrawEllipse(p,29,77,21,21);g.DrawEllipse(p,79,77,21,21);if(index==8)g.DrawLines(p,new[]{new Point(37,41),new Point(50,22),new Point(84,22),new Point(97,41)});}
                    else if(index==9){g.DrawPolygon(p,new[]{new Point(17,77),new Point(111,77),new Point(96,103),new Point(34,103)});g.DrawLine(p,64,22,64,76);g.DrawPolygon(p,new[]{new Point(60,24),new Point(28,66),new Point(60,66)});}
                    else if(index==10){g.DrawPolygon(p,new[]{new Point(64,17),new Point(106,104),new Point(64,86),new Point(22,104)});g.DrawEllipse(p,54,45,20,24);}
                    else {g.DrawRectangle(p,24,26,80,76);g.DrawLine(p,24,50,104,50);g.DrawRectangle(p,54,41,20,26);}
                }else{
                    if(index<=4){int count=index==0?1:index==1?3:2;for(int i=0;i<count;i++){int x=count==1?49:count==2?35+i*31:17+i*31;g.DrawRectangle(p,x,53,29,49);g.DrawPolygon(p,new[]{new Point(x-3,51),new Point(x+14,29),new Point(x+32,51)});}if(index==3){g.DrawRectangle(p,38,28,52,76);g.DrawLine(p,34,25,94,25);}if(index==4)g.DrawLine(p,25,86,93,39);}
                    else if(index==5){g.DrawArc(p,19,23,90,92,180,180);g.DrawLine(p,19,69,19,105);g.DrawLine(p,109,69,109,105);g.DrawArc(p,39,53,50,80,180,180);}
                    else if(index==6||index==13){g.DrawLine(p,28,26,100,102);g.DrawLine(p,100,26,28,102);g.DrawArc(p,15,15,75,62,190,115);g.DrawArc(p,38,15,75,62,235,115);}
                    else if(index==7){g.DrawPolygon(p,new[]{new Point(64,24),new Point(111,103),new Point(17,103)});g.DrawLine(p,64,55,85,103);g.DrawLine(p,64,55,43,103);}
                    else if(index==8){g.DrawArc(p,31,40,66,63,0,180);g.DrawLine(p,64,25,64,105);g.DrawEllipse(p,54,16,20,20);g.DrawLine(p,34,60,94,60);}
                    else if(index==9){g.DrawArc(p,13,35,102,91,180,180);g.DrawLine(p,14,81,114,81);g.DrawLine(p,35,45,35,81);g.DrawLine(p,93,45,93,81);}
                    else if(index==10){g.DrawPolygon(p,new[]{new Point(17,43),new Point(64,19),new Point(111,43)});g.DrawLine(p,20,106,108,106);foreach(int x in new[]{32,64,96})g.DrawLine(p,x,49,x,98);}
                    else if(index==11){g.DrawPolygon(p,new[]{new Point(64,18),new Point(112,105),new Point(16,105)});g.DrawLine(p,64,45,64,75);g.FillEllipse(Brushes.WhiteSmoke,60,87,8,8);}
                    else if(index==12){g.DrawRectangle(p,24,49,80,51);g.DrawArc(p,24,17,80,65,180,180);g.DrawRectangle(p,56,59,16,22);}
                    else if(index==14){foreach(int x in new[]{32,64,96}){g.DrawLine(p,x,30,x,106);g.DrawLine(p,x-13,52,x+13,52);}}
                    else if(index==15){g.DrawEllipse(p,27,16,74,74);g.DrawLines(p,new[]{new Point(33,75),new Point(64,112),new Point(95,75)});g.DrawEllipse(p,51,40,26,26);}
                    else if(index==16){g.DrawEllipse(p,30,17,68,94);g.DrawEllipse(p,46,32,36,64);}
                    else if(index==17){g.DrawLine(p,64,19,64,109);g.DrawLine(p,21,62,107,62);g.DrawLine(p,43,99,85,99);}
                    else if(index==18){g.DrawRectangle(p,48,43,32,42);g.DrawRectangle(p,16,32,25,64);g.DrawRectangle(p,87,32,25,64);g.DrawLine(p,42,64,86,64);}
                    else {g.DrawEllipse(p,34,34,60,60);foreach(int a in Enumerable.Range(0,8)){double angle=a*Math.PI/4;g.DrawLine(p,64+(float)Math.Cos(angle)*37,64+(float)Math.Sin(angle)*37,64+(float)Math.Cos(angle)*50,64+(float)Math.Sin(angle)*50);}}
                }return IconAssets.FromBitmap(bitmap,name);
            }
        }
    }
}
