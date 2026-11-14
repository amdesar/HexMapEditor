using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class QuickRemovalDialog:EditorDialog
    {
        readonly RadioButton quick=new RadioButton{Text="Usuń z szybkiego dostępu",Checked=true,AutoSize=true};
        readonly RadioButton library=new RadioButton{Text="Usuń z biblioteki",AutoSize=true};
        internal bool FromLibrary{get{return library.Checked;}}
        internal QuickRemovalDialog(bool available):base("Usuń wybrany zasób",190)
        {
            library.Enabled=available;Row(0,"",quick,true);Row(1,"",library,true);
            var hint=new Label{Dock=DockStyle.Fill,Text=available?"Wybierz operację. Usunięcie biblioteczne wymaga osobnego potwierdzenia.":"To kopia osadzona w presecie. Nie ma samodzielnego pliku w bibliotece."};Layout.Controls.Add(hint,0,2);Layout.SetColumnSpan(hint,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));Buttons(3,()=>DialogResult=DialogResult.OK);
        }
    }
    public sealed partial class MainWindow
    {
        void ChooseIconRemoval(){ChooseQuickRemoval(false);}
        void ChooseObjectRemoval(){ChooseQuickRemoval(true);}
        void ChooseQuickRemoval(bool objects)
        {
            string id=objects?(objectMaterial?activeObject:null):(iconMaterial?activeIcon:null);if(id==null)return;
            bool exists=objects?libraryObjects.Any(o=>o.Id==id):libraryIcons.Any(i=>i.Id==id);
            using(var d=new QuickRemovalDialog(exists))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK){if(!d.FromLibrary)RemoveQuickEntry(id,objects);else if(objects)DeleteObjectTemplate();else DeleteLibraryIcon();}
        }
    }
}
