using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal enum GraphicLibraryKind { Terrain, Icon, Object, Background }

    internal sealed class LibraryListing : TableLayoutPanel
    {
        static readonly int[] sorting=new int[4];
        static readonly CultureInfo polish=CultureInfo.GetCultureInfo("pl-PL");
        readonly GraphicLibraryKind kind;
        sealed class SearchBox : DarkTextBox
        {
            protected override bool ProcessCmdKey(ref Message message,Keys key)
            {
                if(key==Keys.Escape&&TextLength>0){Clear();return true;}
                return base.ProcessCmdKey(ref message,key);
            }
        }
        readonly TextBox search=new SearchBox { AccessibleName="Szukaj po nazwie zasobu", MaxLength=200 };
        readonly ComboBox order=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Sortowanie biblioteki" };
        readonly Label empty=new Label { Text="Brak pasujących zasobów.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Visible=false };

        internal LibraryListing(GraphicLibraryKind kind,Control content,Action refresh)
        {
            this.kind=kind;Dock=DockStyle.Top;Height=39;Padding=new Padding(8,5,8,4);ColumnCount=3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent,66));ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));ColumnStyles.Add(new ColumnStyle(SizeType.Percent,34));
            RowStyles.Add(new RowStyle(SizeType.Percent,100));
            order.Items.AddRange(new object[]{"Własna kolejność","Nazwa A–Z","Nazwa Z–A","Najnowsze","Najstarsze"});
            order.SelectedIndex=sorting[(int)kind];
            var clear=new EditorButton{Text="×",AccessibleName="Wyczyść wyszukiwanie",Dock=DockStyle.Fill,Enabled=false};
            clear.Click+=(s,e)=>{search.Clear();search.Focus();};
            search.TextChanged+=(s,e)=>{clear.Enabled=search.TextLength>0;refresh();};
            order.SelectedIndexChanged+=(s,e)=>{sorting[(int)this.kind]=order.SelectedIndex;refresh();};
            search.Dock=order.Dock=DockStyle.Fill;Controls.Add(search,0,0);Controls.Add(clear,1,0);Controls.Add(order,2,0);
            content.Controls.Add(empty);empty.BringToFront();
        }

        internal List<T> View<T>(IEnumerable<T> entries,Func<T,string> name,Func<T,long> added,Func<T,string> id)
        {
            var result=Filter(entries,search.Text,sorting[(int)kind],name,added,id);
            empty.Visible=result.Count==0;return result;
        }

        internal static List<T> Filter<T>(IEnumerable<T> entries,string query,int mode,Func<T,string> name,Func<T,long> added,Func<T,string> id)
        {
            string needle=Fold(query.Trim());
            if(needle.Length!=0)entries=entries.Where(entry=>Fold(name(entry)).Contains(needle));
            var comparer=StringComparer.Create(polish,true);
            if(mode==1)entries=entries.OrderBy(name,comparer).ThenBy(id,StringComparer.Ordinal);
            if(mode==2)entries=entries.OrderByDescending(name,comparer).ThenBy(id,StringComparer.Ordinal);
            if(mode==3)entries=entries.OrderByDescending(added).ThenBy(id,StringComparer.Ordinal);
            if(mode==4)entries=entries.OrderBy(added).ThenBy(id,StringComparer.Ordinal);
            return entries.ToList();
        }

        static string Fold(string value)
        {
            var chars=new StringBuilder();
            foreach(char letter in value.ToUpperInvariant().Normalize(NormalizationForm.FormD))
                if(CharUnicodeInfo.GetUnicodeCategory(letter)!=UnicodeCategory.NonSpacingMark)chars.Append(letter=='Ł'?'L':letter);
            return chars.ToString();
        }
    }
}
