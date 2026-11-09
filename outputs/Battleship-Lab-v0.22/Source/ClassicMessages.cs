using System;using System.IO;using System.Text;using System.Drawing;using System.Collections.Generic;
namespace FleetLab {
    public sealed class MessageLine {public readonly Point At;public readonly string Text;public MessageLine(int x,int y,string text){At=new Point(x,y);Text=text;}}
    // English131line table430450 and bounded Classic message dispatcher4241D0.
    public sealed class ClassicMessages {
        readonly string[] lines,names;public readonly byte[] OriginalNameRecords;
        public string Line(int index){if(index<0||index>=lines.Length)throw new ArgumentOutOfRangeException("index");return lines[index];}
        public string ShipName(int type){if(type<0||type>=names.Length)throw new InvalidDataException("Unsupported Classic message ship type");return names[type];}
        public ClassicMessages(byte[] language,byte[] shipNames){
            if(language==null||language.Length<2||language.Length>65536)throw new InvalidDataException("Invalid English table length");foreach(byte b in language)if(b!=10&&b!=13&&(b<32||b>126))throw new InvalidDataException("Unsupported English table encoding");
            string text=Encoding.ASCII.GetString(language).Replace("\r\n","\n");if(text.IndexOf('\r')>=0||!text.EndsWith("\n",StringComparison.Ordinal))throw new InvalidDataException("English table line boundary");var split=text.Split('\n');if(split.Length!=132||split[131]!="")throw new InvalidDataException("Expected131English lines");lines=new string[131];Array.Copy(split,lines,131);foreach(string line in lines)if(line.Length>4096)throw new InvalidDataException("English line too long");
            string[] expected={"Select Target Square","Your %s Firing at %s","MISS!","HIT!","You Sunk %s's %s","%s Is Targetting You","%s Is Firing At You","%s Sunk Your %s"};for(int i=0;i<expected.Length;i++)if(lines[107+i]!=expected[i])throw new InvalidDataException("Unsupported Classic message template "+(107+i));
            if(shipNames==null||shipNames.Length!=13*26)throw new InvalidDataException("Expected13native ship-name records");OriginalNameRecords=(byte[])shipNames.Clone();names=new string[13];for(int i=0;i<13;i++){int length=0;while(length<26&&shipNames[i*26+length]!=0){byte c=shipNames[i*26+length];if(c<32||c>126)throw new InvalidDataException("Unsupported ship-name encoding");length++;}if(length==0||length==26)throw new InvalidDataException("Unterminated ship name");names[i]=Encoding.ASCII.GetString(shipNames,i*26,length);}
        }
        public List<MessageLine> Commands(int phase,int local,int shooter,int sunkType,int result,string other){
            if(phase<0||phase>7||local<0||local>1||result<0||result>2)throw new ArgumentOutOfRangeException("phase");if(other==null||other.Length>64)throw new InvalidDataException("Unsupported player name");foreach(char c in other)if(c<32||c>126)throw new InvalidDataException("Unsupported player name");
            int stage=(phase+local*4)%8;string value=null;
            switch(stage){case 0:value=lines[107];break;case 1:case 2:value=Format(lines[108],ShipName(shooter),other);break;case 3:if(result==2)value=Format(lines[111],other,ShipName(sunkType));break;case 4:value=Format(lines[112],other);break;case 5:case 6:value=Format(lines[113],other);break;case 7:if(result==2)value=Format(lines[114],other,ShipName(sunkType));break;}
            var output=new List<MessageLine>();if(value==null)return output;if(value.Length>=100)throw new InvalidDataException("Native message buffer exceeded");int x=local==0?12:276,y=local==0?11:442;
            if(value.Length<=40)output.Add(new MessageLine(x,y,value));else{int at=30;while(at>=0&&value[at]!=' ')at--;if(at<0)throw new InvalidDataException("Native message cannot wrap without a word boundary");output.Add(new MessageLine(x,y,value.Substring(0,at)));output.Add(new MessageLine(x,y+17,value.Substring(at+1)));}return output;
        }
        static string Format(string template,params string[] args){foreach(string arg in args){int at=template.IndexOf("%s",StringComparison.Ordinal);if(at<0)throw new InvalidDataException("Message format arity");template=template.Substring(0,at)+arg+template.Substring(at+2);}if(template.IndexOf('%')>=0)throw new InvalidDataException("Unsupported message conversion");return template;}
    }
    public sealed class FontGlyph {public readonly int Frame;public readonly Point At;public FontGlyph(int frame,int x,int y){Frame=frame;At=new Point(x,y);}}
    // Native styles3/4:94glyphs per style, ASCII33..126. Space uses E's width.
    public sealed class ClassicSpriteFont:IDisposable {
        readonly List<IndexedImage> frames;readonly Bitmap[] bitmaps=new Bitmap[188];
        public ClassicSpriteFont(string graphics,Color[] palette){frames=Assets.Sph(Path.Combine(graphics,"GAMEFONT.SPH"),palette);if(frames.Count!=567||frames[566]!=null)throw new InvalidDataException("Unsupported game font table");try{for(int i=0;i<564;i++)if(frames[i]==null||frames[i].Width>255||frames[i].Height>255)throw new InvalidDataException("Unsupported native font metrics");for(int i=0;i<188;i++){var f=frames[282+i];for(int p=0;p<f.Pixels.Length;p++)f.Visible[p]&=f.Pixels[p]!=0;bitmaps[i]=f.Bitmap();}}catch{Dispose();throw;}}
        public Size Metric(int frame){if(frame<0||frame>=564)throw new ArgumentOutOfRangeException("frame");return new Size(frames[frame].Width,frames[frame].Height);}
        static int BaseFrame(int style){if(style!=3&&style!=4)throw new InvalidDataException("Unsupported game font style");return style*94;}
        int Advance(char c,int style){int start=BaseFrame(style);if(c==' ')return frames[start+36].Width;if(c<33||c>126)throw new InvalidDataException("Unsupported game font character");return frames[start+c-33].Width+(style==4?3:0);}
        public int Measure(string text,int style){BaseFrame(style);if(text==null)throw new ArgumentNullException("text");int width=0;foreach(char c in text)width=checked(width+Advance(c,style));return width;}
        public List<FontGlyph> Layout(int x,int y,string text){return Layout(x,y,text,3);}
        public List<FontGlyph> Layout(int x,int y,string text,int style){int start=BaseFrame(style);if(text==null)throw new ArgumentNullException("text");var result=new List<FontGlyph>();foreach(char c in text){int advance=Advance(c,style);if(c!=' ')result.Add(new FontGlyph(start+c-33,x,y));x=checked(x+advance);}return result;}
        public void Draw(Graphics graphics,MessageLine line){Draw(graphics,line,3);}
        public void Draw(Graphics graphics,MessageLine line,int style){foreach(var glyph in Layout(line.At.X,line.At.Y,line.Text,style)){var image=bitmaps[glyph.Frame-282];if(image==null)throw new ObjectDisposedException("ClassicSpriteFont");graphics.DrawImage(image,new Rectangle(glyph.At,image.Size));}}
        public byte[] Raster(byte[] background,int width,int height,int x,int y,string text){return Raster(background,width,height,x,y,text,3);}
        public byte[] Raster(byte[] background,int width,int height,int x,int y,string text,int style){if(background==null||width<1||height<1||(long)width*height!=background.Length)throw new InvalidDataException("Font raster bounds");var result=(byte[])background.Clone();foreach(var glyph in Layout(x,y,text,style)){var f=frames[glyph.Frame];for(int sy=0;sy<f.Height;sy++)for(int sx=0;sx<f.Width;sx++){int at=sy*f.Width+sx,px=glyph.At.X+sx,py=glyph.At.Y+sy;if(px>=0&&py>=0&&px<width&&py<height&&f.Visible[at]&&f.Pixels[at]!=0)result[py*width+px]=f.Pixels[at];}}return result;}
        // Original423389..423475 centers three style4lines. Runtime retains the
        // accepted Escape pause binding, so it must not advertise native Quit.
        public List<MessageLine> PauseLines(ClassicMessages messages,int width,bool originalPrompts){if(width<1||width>32767)throw new ArgumentOutOfRangeException("width");var result=new List<MessageLine>();string[] text={messages.Line(34),originalPrompts?messages.Line(124):"Escape Also Resumes",messages.Line(127)};for(int i=0;i<3;i++)result.Add(new MessageLine(width/2-Measure(text[i],4)/2,180+i*40,text[i]));return result;}
        internal Bitmap GlyphBitmap(int index){return bitmaps[index];}
        public void Dispose(){for(int i=0;i<bitmaps.Length;i++){if(bitmaps[i]!=null)bitmaps[i].Dispose();bitmaps[i]=null;}}
    }
}
