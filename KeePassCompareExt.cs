using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using KeePass.Plugins;
using KeePass.Util;
using KeePassLib;

namespace KeePassCompare {

    public sealed class KeePassCompareExt : Plugin {

        private ToolStripSeparator m_tsSeparator = null;
        private ToolStripMenuItem m_tsmiMenuItem = null;
        private ToolStripMenuItem m_tsmiMenuItem2 = null;
        private IPluginHost m_host = null;

        public string UpdateUrl = "https://raw.githubusercontent.com/georgekosmidis/KeePassCompare/master/version.txt";

        public override bool Initialize( IPluginHost host ) {
            if ( host == null ) return false;
            m_host = host;

            // Get a reference to the 'Tools' menu item container
            ToolStripItemCollection tsMenu = m_host.MainWindow.ToolsMenu.DropDownItems;

            // Add a separator at the bottom
            m_tsSeparator = new ToolStripSeparator();
            tsMenu.Add( m_tsSeparator );

            // Add menu item 'KeePassCompare'
            m_tsmiMenuItem = new ToolStripMenuItem();
            m_tsmiMenuItem.Text = "KeePassCompare Compare!";
            m_tsmiMenuItem.Click += this.OnMenuCompare;
            tsMenu.Add( m_tsmiMenuItem );

            // Add menu item 'KeePassCompare'
            m_tsmiMenuItem2 = new ToolStripMenuItem();
            m_tsmiMenuItem2.Text = "KeePassCompare Reset Colours";
            m_tsmiMenuItem2.Click += this.OnResetColors;
            tsMenu.Add( m_tsmiMenuItem2 );

            return true;
        }

        public override void Terminate() {
            // Remove all of our menu items
            ToolStripItemCollection tsMenu = m_host.MainWindow.ToolsMenu.DropDownItems;
            m_tsmiMenuItem.Click -= this.OnMenuCompare;
            m_tsmiMenuItem2.Click -= this.OnResetColors;
            tsMenu.Remove( m_tsmiMenuItem );
            tsMenu.Remove( m_tsmiMenuItem2 );
            tsMenu.Remove( m_tsSeparator );

        }

        private void OnResetColors( object sender, EventArgs e ) {
            var d = m_host.MainWindow.DocumentManager.Documents;
            for ( int i=0; i < d.Count; i++ ) {
                var g = d[i].Database.RootGroup.GetEntries( true );
                foreach ( var ent in g ) {
                    if ( ent.BackgroundColor == System.Drawing.Color.OrangeRed )
                        ent.BackgroundColor = System.Drawing.Color.White;
                }

            }
            m_host.MainWindow.UpdateUI( false, null, true, null, true, null, false );
            MessageBox.Show( "Done!" + Environment.NewLine + "Save the DB to keep changes!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning );
        }
        private void OnMenuCompare( object sender, EventArgs e ) {
            var d = m_host.MainWindow.DocumentManager.Documents;
            if ( d.Count <= 1 ) {
                MessageBox.Show( "You need two databses to compare!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                return;
            }
            if ( d.Count > 2 ) {
                MessageBox.Show( "Only two databases can be compared!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            entries.Clear();

            d[0].Database.RootGroup.SortSubGroups( true );
            d[1].Database.RootGroup.SortSubGroups( true );
            recurse( d[0].Database.RootGroup, d[1].Database.RootGroup );

            if ( entries.Count == 0 )
                MessageBox.Show( "No differences found!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information );
            else {
                var s = "";
                foreach ( var ent in entries ) {
                    var g = ent.ParentGroup;
                    var es = "";
                    while ( g != null ) {
                        es = g.Name + "/" + es;
                        g = g.ParentGroup;
                    }
                    s += es + ent.Strings.Get( "Title" ).ReadString() + Environment.NewLine;
                }

                if (MessageBox.Show( "The following differences were found:" + Environment.NewLine + "(copy to clipboard?)" + Environment.NewLine + s, "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information ) == DialogResult.Yes)
                    System.Windows.Forms.Clipboard.SetText(s);
            }
            m_host.MainWindow.UpdateUI( false, null, true, null, true, null, false );

            Cursor.Current = Cursors.Default;
        }

        private void recurse( PwGroup pw1, PwGroup pw2 ) {
            loop( pw1, pw2 );
            loop( pw2, pw1 );
            foreach ( var g1 in pw1.Groups ) {
                foreach ( var g2 in pw2.Groups ) {
                    if ( g1.Name == g2.Name ) {//compG( g1, g2 )
                        recurse( g1, g2 );
                        break;
                    }
                }
            }
        }

        private List<PwEntry> entries = new List<PwEntry>();
        private void loop( PwGroup g1, PwGroup g2 ) {
            foreach ( var e1 in g1.Entries ) {
                var found = false;
                foreach ( var e2 in g2.Entries ) {
                    if ( compE( e1, e2 ) ) {
                        found = true;
                        break;
                    }
                }
                if ( !found ) {
                    entries.Add( e1 );
                    e1.BackgroundColor = System.Drawing.Color.OrangeRed;
                }
                else {
                    if ( e1.BackgroundColor == System.Drawing.Color.PaleVioletRed )
                        e1.BackgroundColor = System.Drawing.Color.Transparent;
                }
            }
        }

        #region compares
        private PwGroupComparer prgc = new PwGroupComparer();
        private bool compG( PwGroup pw1, PwGroup pw2 ) {
            return prgc.Compare( pw1, pw2 ) > 0;
        }
        private PwEntryComparer prec = new PwEntryComparer( "", false, true );
        private bool compE( PwEntry pe1, PwEntry pe2 ) {
            // return prec.Compare( pe1, pe2 ) > 0;
            //return pe1.LastModificationTime == pe2.LastModificationTime;
            foreach ( var o in pe1.Strings ) {
                if ( !pe2.Strings.Exists( o.Key ) || pe2.Strings.Get( o.Key ).ReadString() != o.Value.ReadString() )
                    return false;
            }
            return true;
        }
        #endregion
    }
}
