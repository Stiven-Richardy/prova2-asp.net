using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Prova2.Desktop.Models;
using Prova2.Desktop.Services;

namespace Prova2.Desktop
{
    public class MainForm : Form
    {
        private readonly UsuarioApiClient _apiClient = new();
        private readonly ToolTip _toolTip = new();
        private int? _usuarioSelecionadoId;
        private bool _senhaVisivel;

        private readonly Color _corPrimaria = ColorTranslator.FromHtml("#0A3D62");
        private readonly Color _corFundo = ColorTranslator.FromHtml("#F4F7F6");
        private readonly Color _corSucesso = ColorTranslator.FromHtml("#198754");
        private readonly Color _corAlerta = ColorTranslator.FromHtml("#D97706");
        private readonly Color _corPerigo = ColorTranslator.FromHtml("#DC3545");
        private readonly Color _corNeutro = ColorTranslator.FromHtml("#6C757D");

        private DataGridView _dgvUsuarios = null!;
        private TextBox _txtBusca = null!;
        private ComboBox _cboFiltroStatus = null!;
        private Button _btnBuscar = null!;

        private Label _lblModoEditor = null!;
        private TextBox _txtNome = null!;
        private TextBox _txtSenha = null!;
        private Button _btnVerSenha = null!;
        private Label _lblDicaSenha = null!;
        private CheckBox _chkStatus = null!;

        private Label _lblRegraTamanho = null!;
        private Label _lblRegraMaiuscula = null!;
        private Label _lblRegraMinuscula = null!;
        private Label _lblRegraNumero = null!;
        private Label _lblRegraEspecial = null!;

        private Button _btnNovo = null!;
        private Button _btnSalvar = null!;
        private Button _btnAlternarStatus = null!;
        private Button _btnExcluir = null!;

        private Label _lblStatusRodape = null!;

        public MainForm()
        {
            ConfigurarJanela();
            ConstruirInterface();
            AtualizarChecklistSenha();
            Load += async (_, _) => await CarregarInicialComRetentativaAsync();
        }

        private void ConfigurarJanela()
        {
            Text = "Gestão de Usuários";
            Size = new Size(1160, 700);
            MinimumSize = new Size(980, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _corFundo;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void ConstruirInterface()
        {
            var pnlTopbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = _corPrimaria,
                Padding = new Padding(22, 0, 22, 0)
            };

            var lblTitulo = new Label
            {
                Text = "GESTÃO DE USUÁRIOS",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlTopbar.Controls.Add(lblTitulo);

            var pnlRodape = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.White,
                Padding = new Padding(18, 0, 18, 0)
            };

            var linhaRodape = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ColorTranslator.FromHtml("#E5E7EB")
            };

            _lblStatusRodape = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = _corNeutro,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Text = "Conectando...",
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lnkDocsApi = new LinkLabel
            {
                Text = "Documentação API",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                LinkColor = ColorTranslator.FromHtml("#94A3B8"),
                ActiveLinkColor = _corPrimaria,
                VisitedLinkColor = ColorTranslator.FromHtml("#94A3B8"),
                LinkBehavior = LinkBehavior.HoverUnderline,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(12, 8, 0, 0),
                Cursor = Cursors.Hand
            };
            lnkDocsApi.LinkClicked += (_, _) => AbrirDocumentacaoApi();
            _toolTip.SetToolTip(lnkDocsApi, "Abrir documentação interativa dos endpoints");

            pnlRodape.Controls.Add(_lblStatusRodape);
            pnlRodape.Controls.Add(lnkDocsApi);
            pnlRodape.Controls.Add(linhaRodape);

            var layoutPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(18)
            };
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

            layoutPrincipal.Controls.Add(CriarPainelListagem(), 0, 0);
            layoutPrincipal.Controls.Add(CriarPainelFormulario(), 1, 0);

            Controls.Add(layoutPrincipal);
            Controls.Add(pnlTopbar);
            Controls.Add(pnlRodape);
        }

        private Panel CriarPainelListagem()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(16)
            };

            var pnlFiltros = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 10)
            };
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));

            _txtBusca = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Buscar usuário por nome...",
                Margin = new Padding(0, 3, 8, 0)
            };
            _txtBusca.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await CarregarUsuariosAsync();
                }
            };

            _cboFiltroStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 3, 8, 0)
            };
            _cboFiltroStatus.Items.AddRange(new object[] { "Todos", "Ativos", "Inativos" });
            _cboFiltroStatus.SelectedIndex = 0;
            _cboFiltroStatus.SelectedIndexChanged += async (_, _) => await CarregarUsuariosAsync();

            _btnBuscar = CriarBotao("Atualizar", _corPrimaria);
            _btnBuscar.Dock = DockStyle.Fill;
            _btnBuscar.Margin = new Padding(0, 1, 0, 1);
            _btnBuscar.Click += async (_, _) => await CarregarUsuariosAsync();

            pnlFiltros.Controls.Add(_txtBusca, 0, 0);
            pnlFiltros.Controls.Add(_cboFiltroStatus, 1, 0);
            pnlFiltros.Controls.Add(_btnBuscar, 2, 0);

            _dgvUsuarios = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                GridColor = ColorTranslator.FromHtml("#EDF2F7")
            };

            _dgvUsuarios.ColumnHeadersDefaultCellStyle.BackColor = _corPrimaria;
            _dgvUsuarios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvUsuarios.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _dgvUsuarios.ColumnHeadersHeight = 32;
            _dgvUsuarios.RowTemplate.Height = 32;
            _dgvUsuarios.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#D6E9F8");
            _dgvUsuarios.DefaultCellStyle.SelectionForeColor = Color.Black;

            _dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "ID",
                DataPropertyName = nameof(UsuarioDto.Id),
                FillWeight = 18
            });
            _dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Nome de Usuário",
                DataPropertyName = nameof(UsuarioDto.Nome),
                FillWeight = 55
            });
            _dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Status",
                DataPropertyName = nameof(UsuarioDto.StatusTexto),
                FillWeight = 27
            });

            _dgvUsuarios.CellFormatting += (_, e) =>
            {
                if (e.ColumnIndex == 2 && e.Value is string statusTxt)
                {
                    e.CellStyle.ForeColor = statusTxt == "Ativo" ? _corSucesso : _corPerigo;
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
            };

            _dgvUsuarios.SelectionChanged += (_, _) => PreencherFormularioComSelecao();

            card.Controls.Add(_dgvUsuarios);
            card.Controls.Add(pnlFiltros);
            return card;
        }

        private Panel CriarPainelFormulario()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(22)
            };

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                AutoScroll = true
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblModoEditor = new Label
            {
                Text = "Novo Usuário",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = _corPrimaria,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblNome = new Label
            {
                Text = "Nome de Usuário (Login)",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 70),
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4)
            };

            _txtNome = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Ex: nome.sobrenome",
                Margin = new Padding(0, 0, 0, 10)
            };

            var lblSenha = new Label
            {
                Text = "Senha de Acesso",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 70),
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4)
            };

            var linhaSenha = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 30,
                Margin = new Padding(0, 0, 0, 3)
            };
            linhaSenha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            linhaSenha.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38f));

            _txtSenha = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = false,
                PasswordChar = '*',
                PlaceholderText = "Digite uma senha forte",
                Margin = new Padding(0, 2, 4, 0)
            };
            _txtSenha.TextChanged += (_, _) => AtualizarChecklistSenha();

            _btnVerSenha = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#F1F5F9"),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1),
                TabStop = false
            };
            _btnVerSenha.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#CBD5E1");
            _btnVerSenha.FlatAppearance.BorderSize = 1;
            _btnVerSenha.Click += (_, _) => AlternarVisibilidadeSenha();
            _btnVerSenha.Paint += DesenharIconeOlho;
            _toolTip.SetToolTip(_btnVerSenha, "Mostrar senha");

            linhaSenha.Controls.Add(_txtSenha, 0, 0);
            linhaSenha.Controls.Add(_btnVerSenha, 1, 0);

            _lblDicaSenha = new Label
            {
                Text = "Obrigatória no cadastro de novos usuários.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            var pnlChecklist = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                BackColor = ColorTranslator.FromHtml("#F8FAFC"),
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlChecklist.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblRegraTamanho = CriarLabelRegra();
            _lblRegraMaiuscula = CriarLabelRegra();
            _lblRegraMinuscula = CriarLabelRegra();
            _lblRegraNumero = CriarLabelRegra();
            _lblRegraEspecial = CriarLabelRegra();

            pnlChecklist.Controls.Add(_lblRegraTamanho, 0, 0);
            pnlChecklist.Controls.Add(_lblRegraMaiuscula, 0, 1);
            pnlChecklist.Controls.Add(_lblRegraMinuscula, 0, 2);
            pnlChecklist.Controls.Add(_lblRegraNumero, 0, 3);
            pnlChecklist.Controls.Add(_lblRegraEspecial, 0, 4);

            _chkStatus = new CheckBox
            {
                Text = "Usuário Ativo (acesso liberado ao portal)",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };

            var gridBotoes = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                Height = 90,
                Margin = new Padding(0, 0, 0, 12)
            };
            gridBotoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            gridBotoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            gridBotoes.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            gridBotoes.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _btnSalvar = CriarBotao("Salvar Usuário", _corPrimaria);
            _btnSalvar.Dock = DockStyle.Fill;
            _btnSalvar.Margin = new Padding(0, 0, 6, 5);
            _btnSalvar.Click += async (_, _) => await SalvarUsuarioAsync();

            _btnNovo = CriarBotao("Limpar / Novo", Color.SlateGray);
            _btnNovo.Dock = DockStyle.Fill;
            _btnNovo.Margin = new Padding(6, 0, 0, 5);
            _btnNovo.Click += (_, _) => LimparFormulario();

            _btnAlternarStatus = CriarBotao("Ativar / Inativar", _corAlerta);
            _btnAlternarStatus.Dock = DockStyle.Fill;
            _btnAlternarStatus.Margin = new Padding(0, 5, 6, 0);
            _btnAlternarStatus.Enabled = false;
            _btnAlternarStatus.Click += async (_, _) => await AlternarStatusSelecionadoAsync();

            _btnExcluir = CriarBotao("Excluir Usuário", _corPerigo);
            _btnExcluir.Dock = DockStyle.Fill;
            _btnExcluir.Margin = new Padding(6, 5, 0, 0);
            _btnExcluir.Enabled = false;
            _btnExcluir.Click += async (_, _) => await ExcluirUsuarioAsync();

            gridBotoes.Controls.Add(_btnSalvar, 0, 0);
            gridBotoes.Controls.Add(_btnNovo, 1, 0);
            gridBotoes.Controls.Add(_btnAlternarStatus, 0, 1);
            gridBotoes.Controls.Add(_btnExcluir, 1, 1);

            var lblInfoAuditoria = new Label
            {
                Text = "Integridade: usuários com histórico em Produtos não podem ser excluídos fisicamente; utilize a opção Ativar / Inativar para revogar o acesso imediatamente.",
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Margin = new Padding(0, 4, 0, 0)
            };

            formLayout.Controls.Add(_lblModoEditor, 0, 0);
            formLayout.Controls.Add(lblNome, 0, 1);
            formLayout.Controls.Add(_txtNome, 0, 2);
            formLayout.Controls.Add(lblSenha, 0, 3);
            formLayout.Controls.Add(linhaSenha, 0, 4);
            formLayout.Controls.Add(_lblDicaSenha, 0, 5);
            formLayout.Controls.Add(pnlChecklist, 0, 6);
            formLayout.Controls.Add(_chkStatus, 0, 7);
            formLayout.Controls.Add(gridBotoes, 0, 8);
            formLayout.Controls.Add(lblInfoAuditoria, 0, 9);

            card.Controls.Add(formLayout);
            return card;
        }

        private static Label CriarLabelRegra()
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.4f, FontStyle.Regular),
                Margin = new Padding(0, 1, 0, 1)
            };
        }

        private static Button CriarBotao(string texto, Color corFundo)
        {
            var btn = new Button
            {
                Text = texto,
                BackColor = corFundo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AlternarVisibilidadeSenha()
        {
            _senhaVisivel = !_senhaVisivel;
            _txtSenha.PasswordChar = _senhaVisivel ? '\0' : '*';
            _toolTip.SetToolTip(_btnVerSenha, _senhaVisivel ? "Ocultar senha" : "Mostrar senha");
            _btnVerSenha.Invalidate();
        }

        private void DesenharIconeOlho(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var cx = btn.Width / 2f;
            var cy = btn.Height / 2f;
            var corIcone = _senhaVisivel ? _corPrimaria : _corNeutro;

            using var pen = new Pen(corIcone, 1.6f);
            using var brush = new SolidBrush(corIcone);
            using var path = new GraphicsPath();
            path.AddBezier(cx - 8f, cy, cx - 4f, cy - 6f, cx + 4f, cy - 6f, cx + 8f, cy);
            path.AddBezier(cx + 8f, cy, cx + 4f, cy + 6f, cx - 4f, cy + 6f, cx - 8f, cy);
            g.DrawPath(pen, path);
            g.FillEllipse(brush, cx - 2.3f, cy - 2.3f, 4.6f, 4.6f);

            if (_senhaVisivel)
            {
                g.DrawLine(pen, cx - 7f, cy + 6f, cx + 7f, cy - 6f);
            }
        }

        private void AtualizarChecklistSenha()
        {
            var senha = _txtSenha.Text;
            var modoEdicaoVazio = _usuarioSelecionadoId.HasValue && string.IsNullOrEmpty(senha);

            AtualizarItemRegra(_lblRegraTamanho, senha.Length >= 8, "Mínimo de 8 caracteres", modoEdicaoVazio);
            AtualizarItemRegra(_lblRegraMaiuscula, senha.Any(char.IsUpper), "Pelo menos 1 letra maiúscula (A-Z)", modoEdicaoVazio);
            AtualizarItemRegra(_lblRegraMinuscula, senha.Any(char.IsLower), "Pelo menos 1 letra minúscula (a-z)", modoEdicaoVazio);
            AtualizarItemRegra(_lblRegraNumero, senha.Any(char.IsDigit), "Pelo menos 1 número (0-9)", modoEdicaoVazio);
            AtualizarItemRegra(_lblRegraEspecial, senha.Any(c => !char.IsLetterOrDigit(c)), "Pelo menos 1 caractere especial (!@#$%...)", modoEdicaoVazio);
        }

        private void AtualizarItemRegra(Label label, bool atendida, string descricao, bool neutro)
        {
            if (neutro)
            {
                label.Text = $"•  {descricao}";
                label.ForeColor = _corNeutro;
                return;
            }

            if (atendida)
            {
                label.Text = $"✔  {descricao}";
                label.ForeColor = _corSucesso;
            }
            else
            {
                label.Text = $"✖  {descricao}";
                label.ForeColor = _corPerigo;
            }
        }

        private static bool SenhaAtendePolitica(string senha)
        {
            return senha.Length >= 8
                && senha.Any(char.IsUpper)
                && senha.Any(char.IsLower)
                && senha.Any(char.IsDigit)
                && senha.Any(c => !char.IsLetterOrDigit(c));
        }

        private void AbrirDocumentacaoApi()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_apiClient.SwaggerUrl) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("Não foi possível abrir o navegador padrão.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task CarregarInicialComRetentativaAsync()
        {
            const int maxTentativas = 10;

            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                DefinirStatus($"Conectando ao servidor ({tentativa}/{maxTentativas})...");
                var (sucesso, dados, mensagem) = await _apiClient.ListarAsync();

                if (sucesso)
                {
                    AtualizarGridERodape(dados);
                    return;
                }

                if (tentativa < maxTentativas)
                {
                    await Task.Delay(500);
                }
                else
                {
                    DefinirStatus(mensagem, erro: true);
                    MessageBox.Show(mensagem, "Falha de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private async Task CarregarUsuariosAsync()
        {
            DefinirStatus("Atualizando lista...");

            bool? filtroStatus = _cboFiltroStatus.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            };

            var (sucesso, dados, mensagem) = await _apiClient.ListarAsync(_txtBusca.Text, filtroStatus);

            if (!sucesso)
            {
                DefinirStatus(mensagem, erro: true);
                MessageBox.Show(mensagem, "Falha de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AtualizarGridERodape(dados);
        }

        private void AtualizarGridERodape(List<UsuarioDto> dados)
        {
            _dgvUsuarios.DataSource = null;
            _dgvUsuarios.DataSource = dados;
            _dgvUsuarios.ClearSelection();

            LimparFormulario();

            var ativos = dados.Count(u => u.Status);
            var inativos = dados.Count - ativos;
            DefinirStatus($"Total: {dados.Count} usuário(s)  |  Ativos: {ativos}  |  Inativos: {inativos}");
        }

        private void PreencherFormularioComSelecao()
        {
            if (_dgvUsuarios.SelectedRows.Count == 0) return;

            if (_dgvUsuarios.SelectedRows[0].DataBoundItem is UsuarioDto usuario)
            {
                _usuarioSelecionadoId = usuario.Id;
                _lblModoEditor.Text = $"Editando Usuário #{usuario.Id}";
                _txtNome.Text = usuario.Nome;
                _txtSenha.Text = string.Empty;
                _lblDicaSenha.Text = "Deixe em branco para manter a senha atual.";
                _chkStatus.Checked = usuario.Status;

                _btnAlternarStatus.Enabled = true;
                _btnExcluir.Enabled = true;
                AtualizarChecklistSenha();
            }
        }

        private void LimparFormulario()
        {
            _usuarioSelecionadoId = null;
            _lblModoEditor.Text = "Novo Usuário";
            _txtNome.Text = string.Empty;
            _txtSenha.Text = string.Empty;
            _lblDicaSenha.Text = "Obrigatória no cadastro de novos usuários.";
            _chkStatus.Checked = true;

            if (_senhaVisivel)
            {
                AlternarVisibilidadeSenha();
            }

            _btnAlternarStatus.Enabled = false;
            _btnExcluir.Enabled = false;
            _dgvUsuarios.ClearSelection();
            AtualizarChecklistSenha();
        }

        private async Task SalvarUsuarioAsync()
        {
            var nome = _txtNome.Text.Trim();
            var senha = _txtSenha.Text;

            if (nome.Length < 2)
            {
                MessageBox.Show("Informe um nome de usuário com pelo menos 2 caracteres.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_usuarioSelecionadoId.HasValue)
            {
                if (!SenhaAtendePolitica(senha))
                {
                    MessageBox.Show("A senha precisa atender a todos os requisitos de segurança exibidos no checklist.", "Senha Fraca", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var (sucesso, mensagem) = await _apiClient.CriarAsync(new CriarUsuarioPayload
                {
                    Nome = nome,
                    Senha = senha,
                    Status = _chkStatus.Checked
                });

                ExibirResultadoECarregar(sucesso, mensagem);
            }
            else
            {
                if (!string.IsNullOrEmpty(senha) && !SenhaAtendePolitica(senha))
                {
                    MessageBox.Show("A nova senha precisa atender a todos os requisitos do checklist (ou deixe o campo vazio para manter a senha atual).", "Senha Fraca", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var (sucesso, mensagem) = await _apiClient.AtualizarAsync(_usuarioSelecionadoId.Value, new AtualizarUsuarioPayload
                {
                    Nome = nome,
                    Senha = string.IsNullOrWhiteSpace(senha) ? null : senha,
                    Status = _chkStatus.Checked
                });

                ExibirResultadoECarregar(sucesso, mensagem);
            }
        }

        private async Task AlternarStatusSelecionadoAsync()
        {
            if (!_usuarioSelecionadoId.HasValue) return;

            var novoStatus = !_chkStatus.Checked;
            var (sucesso, mensagem) = await _apiClient.AtualizarAsync(_usuarioSelecionadoId.Value, new AtualizarUsuarioPayload
            {
                Nome = _txtNome.Text.Trim(),
                Senha = null,
                Status = novoStatus
            });

            ExibirResultadoECarregar(sucesso, mensagem);
        }

        private async Task ExcluirUsuarioAsync()
        {
            if (!_usuarioSelecionadoId.HasValue) return;

            var confirmacao = MessageBox.Show(
                $"Confirma a exclusão do usuário #{_usuarioSelecionadoId.Value} ({_txtNome.Text})?",
                "Confirmação de Exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes) return;

            var (sucesso, mensagem) = await _apiClient.ExcluirAsync(_usuarioSelecionadoId.Value);
            ExibirResultadoECarregar(sucesso, mensagem);
        }

        private async void ExibirResultadoECarregar(bool sucesso, string mensagem)
        {
            if (sucesso)
            {
                DefinirStatus(mensagem);
                await CarregarUsuariosAsync();
            }
            else
            {
                DefinirStatus(mensagem, erro: true);
                MessageBox.Show(mensagem, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DefinirStatus(string texto, bool erro = false)
        {
            _lblStatusRodape.Text = texto;
            _lblStatusRodape.ForeColor = erro ? _corPerigo : _corNeutro;
        }
    }
}