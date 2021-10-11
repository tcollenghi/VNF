var arrExcluiDanfe = [];

function selectRow(id, hora) {

    $("#row" + id).siblings().removeClass("selecionado");
    $("#row" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
    $("#hdfEntrada").val(hora);


    $("#btnVisualizarDetalhe").removeClass("disabled");
}

function mascaraPlaca() {
    setMaskPlaca("txtPlaca");
}

function PulaComEnter(e) {
    if (e.keyCode == 13) {
        VerificaSeExcluilDanfe();
    }
}

function setTable() {
    /* BASIC ;*/
    var responsiveHelper_dt_basic = undefined;
    var responsiveHelper_datatable_fixed_column = undefined;
    var responsiveHelper_datatable_col_reorder = undefined;
    var responsiveHelper_datatable_tabletools = undefined;

    var breakpointDefinition = {
        tablet: 1024,
        phone: 480
    };

    /* COLUMN SHOW - HIDE */
    $('#dttEquipamentos').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
                       "t" +
                       "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dttEquipamentos'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_dt_basic.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_dt_basic.respond();
        }
    });
}


//$("#dttNotas").on("click", ".remover-linha", function () {
//    $(this).closest('tr').remove();
//});

$(".pulaComEnter").keypress(function (e) {
    var tecla = (e.keyCode ? e.keyCode : e.which);
    if (tecla == 13) {
        ConsultarDanfe();
    }
});

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function VerificaSeExcluilDanfe() {
    var erros = "";
    var danfe = $("#txtDanfe").val();
    var notas = "";
    if (danfe == "")
        erros += "<p>-DANFE deve ser informado.</p>";
    if ($.trim(erros) != "") {
        $.SmartMessageBox({
            title: "Erros",
            content: erros,
            buttons: '[Fechar]'
        });
    }
    else {

        $('#dttNotas tbody tr').each(function () {
            if (notas != '')
                notas = notas + ';' + $(this).find('.chave').html();
            else
                notas = $(this).find('.chave').html();
        });


        var arrNotas = notas.split(";");
        setTimeout(function () {
            $.ajax({
                url: url + 'Portaria/VerificaSeExcluilDanfe',
                async: false,
                cache: false,
                type: 'POST',
                dataType: 'json',
                data: {
                    pDanfe: danfe,
                    pNotas: notas
                },
                error: function (jqXHR, error, errorThrown) {
                    $.smallBox({
                        title: "Erros",
                        content: jqXHR.responseText,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {
                    if (oReturn.data == "") {
                        ConsultarDanfe();
                    } else {

                        ExcluilDanfe(oReturn.data)
                        
                    }

                }
            });
        }, 800);
    }
}

function ExcluilDanfe(id) {

    RemoveLinha(id);
    $("#txtDanfe").val("");
    
   
}

function ConsultarDanfe() {
    var erros = "";
    var danfe = $("#txtDanfe").val();

    if (danfe == "")
        erros += "<p>-DANFE deve ser informado.</p>";
    if ($.trim(erros) != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        })
    }
    else {
        //Verifica se a pesquisa tem que ser feita pelo numero da nota ou pela chave de acesso da NFe
        if (danfe.length < 44) {
            $.ajax({
                url: url + 'Portaria/ConsultarDanfeNFe',
                async: false,
                cache: false,
                type: 'GET',
                dataType: 'text',
                data: {
                    pDanfe: danfe
                },
                error: function () {
                    alert(erro);
                },
                success: function (oReturn) {
                    $("#dttPesquisaNotas tbody").html("");
                    $("#dttPesquisaNotas tbody").append(oReturn);
                    $("#modalBusca").modal('show');
                }
            });
        }
        else {
            $.ajax({
                url: url + 'Portaria/ConsultarDanfe',
                async: false,
                cache: false,
                type: 'GET',
                dataType: 'json',
                data: {
                    pDanfe: danfe
                },
                error: function () {
                    alert(erro);
                },
                success: function (oReturn) {

                    if (oReturn.data == "false") {
                        showAlertMessage("VNF", "Documenton não pode ser inserido na pasta, pois já foi integrado ao SAP ou apresenta situação de cancelado ou recusado!");
                    }
                    else {
                        if (oReturn.data.length == 0) {
                            if (Index == 0)
                                $("#dttNotas tfoot").hide();
                            //Index = undefined;
                        } else {
                            $("#dttNotas tbody").append(oReturn.data);
                            $("#dttNotas").removeClass("hide");
                        }
                        $("#txtDanfe").val("");
                        $('#dttNotas').reload();
                    }
                    $("#txtDanfe").val("");
                }
            });
        }


    }
}


function SelecionaNFe(Chave) {
    $("#modalBusca").modal('hide');
    $("#txtDanfe").val(Chave);
    VerificaSeExcluilDanfe();
}
function validarForm() {
    var erros = "";

    var pasta = $("#txtPasta").val();
    var setor = $("#txtSetor").val();
    var transportadora = $("#txtTransportadora").val();
    var motorista = $("#txtMotorista").val();
    var placa = $("#txtPlaca").val();

    if (pasta == "")
        erros += "<p>-Pasta deve ser informada.</p>";
    if (setor == "")
        erros += "<p>-Setor deve ser informado.</p>";
    if (transportadora == "")
        erros += "<p>-Transportadora deve ser informada.</p>";
    if (motorista == "")
        erros += "<p>-Motorista deve ser informado.</p>";
    if (placa == "")
        erros += "<p>-Placa deve ser informada.</p>";


    if ($.trim(erros) != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        })
        return false;
    }
    else {
        return true;
    }
}

function Dados(controller) {

    window.location = url + controller + '/Dados/';
}

function registrarChegada() {

    if (validarForm()) {
        var hdfTipoPortaria = $("#hdfTipoPortaria").val();
        var dataChegada = $("#txtDataChegada").val();
        var pasta = $("#txtPasta").val();
        var setor = $("#txtSetor").val();
        var transportadora = $("#txtTransportadora").val();
        var motorista = $("#txtMotorista").val();
        var placa = $("#txtPlaca").val();
        var notas = '';

        $('#dttNotas tbody tr').each(function () {
            if (notas != '')
                notas = notas + ';' + $(this).find('.chave').html();
            else
                notas = $(this).find('.chave').html();
        });

        $.smallBox({
            title: "Portaria",
            content: "Favor aguarde estamos processando as informações.",
            color: "#739E73",
            icon: "fa fa-cog fa-spin",
            timeout: 5000
        });

        setTimeout(function () {
            $.ajax({
                url: url + 'Portaria/RegistrarChegada',
                async: false,
                cache: false,
                type: 'POST',
                dataType: 'json',
                data: {
                    pUnidade: hdfTipoPortaria,
                    pPasta: pasta,
                    pMotorista: motorista,
                    pPlaca: placa,
                    pSetor: setor,
                    pTransportadora: transportadora,
                    pDataChega: dataChegada,
                    pNotas: notas
                },
                success: function (oReturn) {
                    if (oReturn.data == "") {
                        $('#btnFechar').trigger('click');

                        $.smallBox({
                            title: "Portaria",
                            content: "Chegada Registrada.",
                            color: "#739E73",
                            icon: "fa fa-check-circle",
                            timeout: 4000
                        });
                        $("#btnRegistrarEntrada").removeClass("hidden");
                        $("#btnRegistrarChegada").addClass("hidden");
                        LoadData();
                    }
                    else {
                        $.smallBox({
                            title: "Erros",
                            content: oReturn.data,
                            color: "#C46A69",
                            icon: "fa fa-exclamation-circle",
                            buttons: '[Fechar]',
                            timeout: 4000
                        });
                    }
                },
                error: function (jqXHR, error, errorThrown) {
                    $.smallBox({
                        title: "Erros",
                        content: jqXHR.responseText,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                }
            });
        }, 800);
    }
}

function registrarEntrada() {

    if (validarForm()) {
        var hdfTipoPortaria = $("#hdfTipoPortaria").val();
        var pasta = $("#txtPasta").val();
        var setor = $("#txtSetor").val();
        var transportadora = $("#txtTransportadora").val();
        var motorista = $("#txtMotorista").val();
        var placa = $("#txtPlaca").val();
        var notas = '';

        $('#dttNotas tbody tr').each(function () {
            if (notas != '')
                notas = notas + ';' + $(this).find('.chave').html();
            else
                notas = $(this).find('.chave').html();
        });
        $.smallBox({
            title: "Portaria",
            content: "Favor aguarde estamos processando as informações.",
            color: "#739E73",
            icon: "fa fa-cog fa-spin",
            timeout: 5000
        });

        setTimeout(function () {
            $.ajax({
                url: url + 'Portaria/RegistrarEntrada',
                async: false,
                cache: false,
                type: 'POST',
                dataType: 'json',
                data: {
                    pUnidade: hdfTipoPortaria,
                    pPasta: pasta,
                    pMotorista: motorista,
                    pPlaca: placa,
                    pSetor: setor,
                    pTransportadora: transportadora,
                    pNotas: notas
                },
                error: function (jqXHR, error, errorThrown) {
                    $.smallBox({
                        title: "Erros",
                        content: jqXHR.responseText,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {
                    if (oReturn.data == "") {
                        $('#btnFechar').trigger('click');

                        $("#btnFechar").click();
                        $.smallBox({
                            title: "Portaria",
                            content: "Entrada Registrada.",
                            color: "#739E73",
                            icon: "fa fa-check-circle",
                            timeout: 4000
                        });
                        LoadData();
                    }
                    else {
                        $.smallBox({
                            title: "Erros",
                            content: oReturn.data,
                            color: "#C46A69",
                            icon: "fa fa-exclamation-circle",
                            buttons: '[Fechar]',
                            timeout: 4000
                        });
                    }
                }
            });
        }, 800);
    }
}

function registrarSaida() {

    if (validarForm()) {
        var hdfTipoPortaria = $("#hdfTipoPortaria").val();
        var pasta = $("#txtPasta").val();

        $.smallBox({
            title: "Portaria",
            content: "Favor aguarde estamos processando as informações.",
            color: "#739E73",
            icon: "fa fa-cog fa-spin",
            timeout: 5000
        });

        setTimeout(function () {
            $.ajax({
                url: url + 'Portaria/RegistrarSaida',
                async: false,
                cache: false,
                type: 'POST',
                dataType: 'json',
                data: {
                    pUnidade: hdfTipoPortaria,
                    pPasta: pasta
                },
                error: function (jqXHR, error, errorThrown) {
                    $.smallBox({
                        title: "Erros",
                        content: jqXHR.responseText,
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {
                    if (oReturn.data == "") {
                        $('#btnFechar').trigger('click');
                        $("#btnVisualizarDetalhe").addClass("disabled");

                        $.smallBox({
                            title: "Portaria",
                            content: "Saída Registrada.",
                            color: "#739E73",
                            icon: "fa fa-check-circle",
                            timeout: 4000
                        });

                        LoadData();
                    }
                    else {
                        $.smallBox({
                            title: "Erros",
                            content: oReturn.data,
                            color: "#C46A69",
                            icon: "fa fa-exclamation-circle",
                            buttons: '[Fechar]',
                            timeout: 4000
                        });
                    }
                }
            });
        }, 800);
    }
}

function ExibirMensagem() {
    var hdfMensagem = $("#hdfMensagem").val();

    if (hdfMensagem != "") {
        $.smallBox({
            title: "Alerta",
            content: hdfMensagem,
            timeout: 4000
        });
    }
}

function ExportarEquipamentos(pstrPortaria) {
    $("#divPostingPage").removeClass("hide");
    $("#divPostingPage").show();
    $('html, body').animate({ scrollTop: 0 }, 'slow');

    numeroPasta = $("#txtNumeroPasta").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Portaria/ExportarNeles",
        dataType: 'html',
        data: {
            pNumeroPasta: numeroPasta,
            pstrPortaria: $("#hdfTipoPortaria").val() 
        },
        error: function () {
            $("#divPostingPage").addClass("hide");
        },
        success: function (oReturn) {
            $("#divPostingPage").addClass("hide");
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

function LoadData() {
    var txtNumeroPasta = $("#txtNumeroPasta").val();
    //alert($("#hdfTipoPortaria").val());
    if ($("#hdfTipoPortaria").val() == "PTNL") {
        $.ajax({
            async: false,
            cache: false,
            type: 'GET',
            url: url + "Portaria/LoadDataNeles",
            dataType: 'html',
            data: {
                pNumeroPasta: txtNumeroPasta,
                r: Math.random()
            },
            error: function (erro) {
                alert(erro);
            },
            success: function (oReturn) {
                $("#divGrid").empty();
                $("#divGrid").html(oReturn);
            }
        });

    } else if ($("#hdfTipoPortaria").val() == "FUND") {
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Portaria/LoadDataFundicao",
            dataType: 'html',
            data: {
                pNumeroPasta: txtNumeroPasta,
                r: Math.random()
            },
            error: function (erro) {
                alert(erro);
            },
            success: function (oReturn) {
                $("#divGrid").empty();
                $("#divGrid").html(oReturn);
            }
        });
    }
    else {
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Portaria/LoadDataPedroLeopodo",
            dataType: 'html',
            data: {
                pNumeroPasta: txtNumeroPasta,
                r: Math.random()
            },
            error: function (erro) {
                alert(erro);
            },
            success: function (oReturn) {
                $("#divGrid").empty();
                $("#divGrid").html(oReturn);
            }
        });}
}

function relatorioDivergencias() {
    ShowLoading("Aguarde", "Gerando relatório...");

    var notas = '';
    $('#dttNotas tbody tr').each(function () {
        if (notas != '')
            notas = notas + ';' + $(this).find('.chave').html();
        else
            notas = $(this).find('.chave').html();
    });

    if (notas == '') {
        $.SmartMessageBox({
            title: "Erros",
            content: "Não existem notas para gerar um relatório.",
            buttons: '[Fechar]'
        }, function (ButtonPress, Value) {
            HideLoading();
            return;
        })
    } else {
        window.location = url + "Portaria/RelatorioDivergencias?pNotas=" + notas;
        HideLoading();
    }
}

function VisualizarDetalhes() {
    $("#txtPasta").val('');
    $("#txtSetor").val('');
    $("#txtTransportadora").val('');
    $("#txtMotorista").val('');
    $("#txtPlaca").val('');
    $("#dttNotas").hide();

    hdfTipoPortaria = $("#hdfTipoPortaria").val();
    if ($("#hdfId").val() == "") {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione uma nota.",
            buttons: '[Fechar]'
        })
        return;
    }
    pasta = $("#hdfId").val();
    unidade = hdfTipoPortaria;


    $.smallBox({
        title: "Portaria",
        content: "Favor aguarde estamos procurando as informações.",
        color: "#739E73",
        icon: "fa fa-cog fa-spin",
        timeout: 2000
    });

    setTimeout(function () {
        $.ajax({
            async: false,
            cache: false,
            type: 'GET',
            url: url + "Portaria/Pasta",
            dataType: 'html',
            data: {
                pPasta: pasta,
                pUnidade: unidade

            },
            error: function (erro) {
                alert(erro);
            },
            success: function (oReturn) {
                $("#dttNotas").show();
                $("#definicaoTabela").html(oReturn);
                if ($("#hdfEntrada").val() == "") {
                    $("#btnAlterarChegada").removeClass("hidden");
                    $("#btnRegistrarEntrada").removeClass("hidden");
                    $("#btnRegistrarChegada").addClass("hidden");

                } else {
                    $("#btnRegistrarEntrada").addClass("hidden");

                    $("#btnRegistrarChegada").addClass("hidden");
                    $("#btnRegistrarSaida").removeClass("hidden");

                    $("#divDanfeVerifica").hide();
                    $(".txtPasta").css('height', '100px');
                    $(".txtPasta").css('line-height', '0%');
                }
            }
        });
    }, 600);
}

function LimpaModal() {
    var hdfTipoPortaria = $("#hdfTipoPortaria").val();
    pasta = "";
    unidade = hdfTipoPortaria;

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Portaria/Pasta",
        dataType: 'html',
        data: {
            pPasta: pasta,
            pUnidade: unidade

        },
        error: function (erro) {
            alert(erro);
        },
        success: function (oReturn) {
            $("#definicaoTabela").html(oReturn);
            $("#btnRegistrarChegada").removeClass("hidden");
            $("#btnRegistrarEntrada").addClass("hidden");
            $("#btnRegistrarSaida").addClass("hidden");

        }
    });
}


function RemoveLinha(Id) {
    $.SmartMessageBox({
        title: "VNF NELES",
        content: "Deseja remover a NF-e?",
        buttons: '[No][Yes]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Yes") {
            $("#" + Id).remove();
        }
    });

}