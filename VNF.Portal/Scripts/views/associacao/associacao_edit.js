var FalhaAdicionarPedidoItem = false;
var ItemRefNF;
$(document).ready(function () {

    pageSetUp();
    setMaskCnpj("cnpjDestinatario");
    setMaskCnpj("cnpj");

    LoadDivergencia();
    LoadDatasComparacoes();

    var table = document.getElementById("dttTabela");
    if (table != undefined) {
        for (var i = 0, row; row = table.rows[i]; i++) {
            var id = "";
            id = row.id;
            id = id.replace("row_", "");
            if (id != "") {
                if ( $("#hfRelevante_" + id).val() == "X") {
                    $("#txtPedido_" + id).css("color", "#909090");
                    $("#txtPedido_" + id).attr("disabled", "disabled");
                    $("#txtItemPedido_" + id).css("color", "#909090");
                    $("#txtItemPedido_" + id).attr("disabled", "disabled");
                } else {
                    $("#txtPedido_" + id).css("color", "#c26565");
                    $("#txtPedido_" + id).removeAttr("disabled");
                    $("#txtItemPedido_" + id).css("color", "#c26565");
                    $("#txtItemPedido_" + id).removeAttr("disabled");
                }
            }
        }
    }
    if ($("#hfmodificar").val() == "False")
    {
        
        $("#btnAssociar").attr("disabled", "disabled");
        $("#btnAssociar").css("color", "#909090");
        showAlertMessage("VNF", "Não será possível alterar a informação pois a integração com o SAP está concluída, ou o documento encontra-se com status de recusado ou cancelado.");
        //$.smallBox({
        //    title: "VNF NELES",//////
        //    content: "Não será possível alterar a informação pois a integração com o SAP está concluída, ou o documento encontra-se com status de recusado ou cancelado.",
        //    color: "#C46A69",
        //    icon: "fa fa-lock fadeInLeft animated",
        //    timeout: 4000
        //});
    }

})

function selectRowPedido(id, pedido) {
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    if ($("#row_" + id).hasClass("selecionado"))
    {
        $("#hdfId").val(id);
        $("#hdfPedido").val(pedido);
    } else {
        $("#hdfId").val("");
        $("#hdfPedido").val("");
    }
}

function LoadDivergencia() {
    $("#divDivergencia").empty();
    IdNfe = $("#VNF_CHAVE_ACESSO").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDataDivergenciaNfe",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
            $.smallBox({
                title: "VNF NELES",
                content: "Ocorreu um erro ao carregar as divergências",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.data.length == 0) {
                $("#divDivergencia").append("não existe informação");
                $("#lblPendentes").hide();
                Index = undefined;
            } else {
                $("#divDivergencia").append(oReturn.data);
                $("#lblPendentes").append(oReturn.pendentes);
                if (oReturn.pendentes > 0) {
                    $("#lblPendentes").show();
                } else {
                    $("#lblPendentes").hide();
                }
            }
        }
    });
}

function LoadDatasComparacoes() {
    $("#dttDatasComparacoes tbody").empty();
    $("#dttDatasComparacoes tfoot").show();

    IdNfe = $("#VNF_CHAVE_ACESSO").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDatasComparacoes",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
            $.smallBox({
                title: "VNF NELES",
                content: "Ocorreu um erro ao carregar as comparações",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.data.length == 0) {
                $("#dttDatasComparacoes tbody").append("<tr><td colspan='2' class='center'>não existe informação</td></tr>");
                $("#dttDatasComparacoes tfoot").hide();
                Index = undefined;
            } else {
                $("#dttDatasComparacoes tbody").append(oReturn.data);
                $("#dttDatasComparacoes tfoot").hide();
            }
        }
    });
}

function LoadComparacoes(pData) {

    var res = pData.split(" ");

    $("#row" + res[0].replace('/', '').replace('/', '') + res[1].replace(':', '').replace(':', '')).siblings().removeClass("selecionado");
    $("#row" + res[0].replace('/', '').replace('/', '') + res[1].replace(':', '').replace(':', '')).toggleClass("selecionado");

    $("#divComparacoes").empty();
    $("#divComparacoes").append("<span>carregando...</span>");

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadComparacoes",
        dataType: 'json',
        data: {
            pData: pData,
            pNfeID: $("#hfIdNfe").val()
        },
        error: function () {
        },
        success: function (oReturn) {
            $("#divComparacoes").empty();
            if (oReturn.data.length == 0) {
                $("#divComparacoes").append("<span>não existe informação</span>");
                Index = undefined;
            } else {
                $("#divComparacoes").append(oReturn.data);
            }
        }
    });
}

function GetXml() {
    IdNfe = $("#hfIdNfe").val();
    var urlXml = url + "Associacao/GetXml/" + IdNfe;
    window.open(urlXml, '_blank');
}

function GetDanfe() {
    if ($("#VNF_TIPO_DOCUMENTO").val() == "TAL") {
        var GetDanteUrl = url + 'Talonario/DownloadAnexoChave?id=' + $('#VNF_CHAVE_ACESSO').val();
        window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    } else {
        var GetDanteUrl = url + 'Compras/DownloadDanfe?pIdNfe=' + $('#VNF_CHAVE_ACESSO').val();
        window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    }
}

//copia o numero do pedido em todos os inputs de pedido que esteja em branco.
function CopiarNumeroPedido(textInput, Id) {
    var statusIntegracao = $("#hfStatusIntegracao").val();
    
    if (statusIntegracao == "CONCLUÍDO") {
        $("#" + textInput.id).val(textInput.defaultValue);
        $.smallBox({
            title: "VNF NELES",
            content: "Não é possível alterar a informação pois a integração com o SAP está concluída.",
            color: "#C46A69",
            icon: "fa fa-lock fadeInLeft animated",
            timeout: 4000
        });
    } else {
        var table = document.getElementById("dttTabela");
        var Counter = 0;
        var pedido = $("#txtPedido_" + Id).val();
        for (var i = 0, row; row = table.rows[i]; i++) {
            var aux = "";
            aux = row.id;
            aux = aux.replace("row_", "");
            if (aux != "") {
                if ($("#txtPedido_" + aux).val() == "") {
                    $("#txtPedido_" + aux).val(pedido);
                }
            }
        }
    }
}

function VerificarStatusIntegracao(textInput) {
    var statusIntegracao = $("#hfStatusIntegracao").val();
    if (statusIntegracao == "CONCLUÍDO") {
        $("#" + textInput.id).val(textInput.defaultValue);
        $.smallBox({
            title: "VNF NELES",
            content: "Não é permitido fazer a associação desta NF pois o processo de integração foi concluído.",
            color: "#C46A69",
            icon: "fa fa-lock fadeInLeft animated",
            timeout: 4000
        });
    }
}

function AddPedidoMassa(showAlert) {
    var table = document.getElementById("dttTabela");
    var Counter = 0;

    if (table != undefined) {
        for (var i = 0, row; row = table.rows[i]; i++) {
            var aux = "";
            aux = row.id;
            aux = aux.replace("row_", "");
            if (aux != "") {
                var Id = parseInt(aux);
                AddPedidoItem(Id, 0);
                if (FalhaAdicionarPedidoItem == true) {
                    break;
                }
                Counter++;
            }
        }

        if (showAlert == true && Counter > 0) {
            $.smallBox({
                title: "VNF NELES",
                content: "Itens salvos com sucesso!",
                color: "#739E73",
                icon: "fa fa-check-circle",
                timeout: 4000
            });
        }
    }
}


function AddPedidoItem(itemNota, Message) {
    var txtIdNF = $("#VNF_CHAVE_ACESSO").val();
    var txtPedido = $("#txtPedido_" + itemNota).val();
    var txtItemPedido = $("#txtItemPedido_" + itemNota).val();

    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + "Associacao/AddPedidoItem",
        dataType: 'text',
        data: {
            txtIdNF: txtIdNF,
            itemNota: itemNota,
            txtPedido: txtPedido,
            txtItemPedido: txtItemPedido
        },
        error: function () {
            FalhaAdicionarPedidoItem = true;
            if (Message == 1) {
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao salvar as informações da associação",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            }
        },
        success: function (oReturn) {
            if (Message == 1) {
                if (oReturn != "") {
                    $.SmartMessageBox({
                        title: "Não é possível associar este documento",
                        content: oReturn,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                } else {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Item salvo com sucesso!",
                        color: "#739E73",
                        icon: "fa fa-check-circle",
                        timeout: 4000
                    });
                }
            }
        }
    });
}


function AddNotaReferenciada() {
    var txtIdNF = $("#VNF_CHAVE_ACESSO").val();
    var txtNumero = $("#NF_NFREF_REFNNF").val();
    var txtSerie = $("#NF_NFREF_REFSerie").val();

    if ($("#hfIsMandatoryNotaFiscalReferenciada").val() == "1") {
        ShowLoading("Aguarde", "validando informações da nota fiscal referenciada");
        var errorMsg = "";
        if ($.trim(txtNumero) === "" || $.trim(txtSerie) === "") errorMsg = "Os campos Número e Série da nota fiscal referenciadas são obrigatórios";
        if ($.trim(txtNumero) === $.trim($("#NF_IDE_NNF").val()) && $.trim(txtSerie) === $.trim($("#NF_IDE_SERIE").val())) errorMsg = "A nota fiscal referenciadas deve ser diferente da nota de remessa";

        if ($.trim(errorMsg)) {
            $.SmartMessageBox({
                title: "Não foi possível referenciar este documento",
                content: errorMsg,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
            return;
        }

        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Associacao/AddNotaReferenciada",
            dataType: 'text',
            data: {
                txtIdNF: txtIdNF,
                txtNumero: txtNumero,
                txtSerie: txtSerie
            },
            error: function () {
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao salvar as informações da nota referenciada",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                if (oReturn != "") {
                    $.SmartMessageBox({
                        title: "Não foi possível referenciar este documento",
                        content: oReturn,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                } else {
                    HideLoading();
                    AssociarStep(2);
                }
            }
        });
    } else {
        AssociarStep(2);
    }
}

function ConsultarPedido() {
    ShowLoading("Aguarde", "Carregando informações do pedido");
    var hdfPedido = $("#hdfPedido").val();
    var itemNF = $("#hdfId").val();

    if (hdfPedido == "" || itemNF == "") {
        $.SmartMessageBox({
            title: "Consultar pedido",
            content: "Selecione uma linha com pedido e item para efetuar a consulta.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
            return;
        });
    } else {
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Associacao/ConsultarPedido",
            dataType: 'json',
            data: {
                nfeid: $("#VNF_CHAVE_ACESSO").val(),
                itemNf: itemNF
            },
            error: function () {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao consultar o pedido",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                HideLoading();
                if (oReturn.result == "Ok") {
                    $("#dttConsultaPedido tfooter").empty();
                    $("#dttConsultaPedido tbody").empty();
                    $("#dttConsultaPedido tbody").append(oReturn.data);
                    $("#divConsultarPedido").modal();
                }
                else {
                    $.smallBox({
                        title: "Consultar Pedido",
                        content: oReturn.data,
                        color: "#C46A69",
                        iconSmall: "fa fa-times fa-2x fadeInRight animated",
                        timeout: 4000
                    });
                }
            }
        });
    }
}

function AssociarStep(step) {
    switch (step) {
        case 0:
            FalhaAdicionarPedidoItem = false;
            AddPedidoMassa(false);
            if (FalhaAdicionarPedidoItem == true) {
                $.SmartMessageBox({
                    title: "Ocorreu uma falha ao salvar o pedido e item para a Nota Fiscal.",
                    content: oReturn,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            AssociarStep(1);
            break;
        case 1:
            AddNotaReferenciada();
            break;
        case 2:
            EnviarAssociarNF();
            break;
    }
}

function Associar() {
    AssociarStep(0);
}

function EnviarAssociarNF() {
    //FalhaAdicionarPedidoItem = false;
    //AddPedidoMassa(false);

    //if (FalhaAdicionarPedidoItem == true) {
    //    $.SmartMessageBox({
    //        title: "Ocorreu uma falha ao salvar o pedido e item para a Nota Fiscal.",
    //        content: oReturn,
    //        buttons: '[Ok]'
    //    }, function (ButtonPress, Value) {
    //        HideLoading();
    //    });
    //} else {
    ShowLoading("Aguarde", "validando informações da nota fiscal e pedido de compra");
    txtIdNF = $("#VNF_CHAVE_ACESSO").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/AssociarNF",
        dataType: 'json',
        data: {
            txtIdNF: txtIdNF
        },
        error: function () {
            $.SmartMessageBox({
                title: "Ocorreu um erro durante a associação do documento.",
                content: "Tente novamente mais tarde",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.mensagem != "") {
                if (oReturn.status == "ACEITA") {
                    $("#VNF_STATUS").val(oReturn.status);
                    $("#lblStatus").removeClass("state-error");
                    $("#lblStatus").addClass("state-success");
                } else if (oReturn.status == "REJEITADA") {
                    $("#VNF_STATUS").val(oReturn.status);
                    $("#lblStatus").removeClass("state-success");
                    $("#lblStatus").addClass("state-error");
                } else {
                    $("#VNF_STATUS").val(oReturn.status);
                    $("#lblStatus").removeClass("state-success");
                    $("#lblStatus").removeClass("state-error");
                }

                $.SmartMessageBox({
                    title: "Não é possível associar este documento",
                    content: oReturn.mensagem,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            } else {
                window.location.reload();
            }
        }
    });
    //}
}

function AlterarRelevancia(id) {
    ShowLoading("Aguarde", "Atualizando relevância do item.")
    var textoAtualizacao = "";
    if ($("#hfRelevante_" + id).val() == "X") {
        $("#hfRelevante_" + id).val("N");
        textoAtualizacao = "Item atualizado para relevante.";
    } else {
        $("#hfRelevante_" + id).val("X");
        textoAtualizacao = "Item atualizado para irrelevante.";
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/AlterarRelevancia",
        dataType: 'json',
        data: {
            txtIdNF: $("#VNF_CHAVE_ACESSO").val(),
            txtItemNF: id,
            ItemRelevante: $("#hfRelevante_" + id).val()
        },
        error: function () {
            HideLoading();
            $.smallBox({
                title: "VNF NELES",
                content: "Não foi possível alterar a relevância do item",
                color: "#C46A69",
                icon: "fa fa-times fadeInLeft animated",
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.mensagem != "") {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: oReturn.mensagem,
                    color: "#C46A69",
                    icon: "fa fa-times fadeInLeft animated",
                    timeout: 4000
                });

                if (oReturn.status != "") {
                    if (oReturn.status == "ACEITA") {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-error");
                        $("#lblStatus").addClass("state-success");
                    } else if (oReturn.status == "REJEITADA") {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-success");
                        $("#lblStatus").addClass("state-error");
                    } else {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-success");
                        $("#lblStatus").removeClass("state-error");
                    }
                }

                if (oReturn.bloquear == true) {
                    if ($("#hfRelevante_" + id).val() == "X") {
                        $("#hfRelevante_" + id).val("N");
                        $("#chkItemRelevante_" + id).prop("checked", true);
                    } else {
                        $("#hfRelevante_" + id).val("X");
                        $("#chkItemRelevante_" + id).removeAttr("checked");
                    }
                }
            } else {
                HideLoading();
                if ($("#hfRelevante_" + id).val() == "X") {
                    $("#txtPedido_" + id).css("color", "#909090");
                    $("#txtPedido_" + id).attr("disabled", "disabled");
                    $("#txtItemPedido_" + id).css("color", "#909090");
                    $("#txtItemPedido_" + id).attr("disabled", "disabled");
                    $("#tdAssociacao_" + id).empty();
                    $("#tdAssociacao_" + id).append("<h5 title='Irrelevante'><i class='fa fa-info-circle txt-color-yellow'></i></h5>");
                    $("#VNF_STATUS").val("ACEITA");
                    $("#lblStatus").removeClass("state-error");
                    $("#lblStatus").addClass("state-success");
                } else {
                    $("#txtPedido_" + id).css("color", "#c26565");
                    $("#txtPedido_" + id).removeAttr("disabled");
                    $("#txtItemPedido_" + id).css("color", "#c26565");
                    $("#txtItemPedido_" + id).removeAttr("disabled");
                    $("#tdAssociacao_" + id).empty();
                    if ($("#hfRelDefaultValue_" + id).val() == "S") {
                        $("#tdAssociacao_" + id).append("<h5 title='Associado'><i class='fa fa-check-circle txt-color-green'></i></h5>");
                    } else {
                        $("#tdAssociacao_" + id).append("<h5 title='Pendente'><i class='fa fa-minus-circle txt-color-red'></i></h5>");
                    }
                }

                if (oReturn.status != "") {
                    if (oReturn.status == "ACEITA") {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-error");
                        $("#lblStatus").addClass("state-success");
                    } else if (oReturn.status == "REJEITADA") {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-success");
                        $("#lblStatus").addClass("state-error");
                    } else {
                        $("#VNF_STATUS").val(oReturn.status);
                        $("#lblStatus").removeClass("state-success");
                        $("#lblStatus").removeClass("state-error");
                    }
                }
                
                $.smallBox({
                    title: "VNF NELES",
                    content: textoAtualizacao,
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    timeout: 4000
                });
            }
        }
    });
}


$(".modal-wide").on("show.bs.modal", function () {
    var height = $(window).height() - 200;
    $(this).find(".modal-body").css("max-height", height);
});



function ConsultarInbound(refreshData) {
    ShowLoading("Aguarde", "Carregando informações da inbound");
    var hdfPedido = $("#hdfPedido").val();
    var itemNF = $("#hdfId").val();

    if (hdfPedido == "" || itemNF == "") {
        $.SmartMessageBox({
            title: "Consultar inbound",
            content: "Selecione uma linha com pedido e item para efetuar a consulta.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
            return;
        });
    } else {
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Associacao/ConsultarInbound",
            dataType: 'json',
            data: {
                nfeid: $("#VNF_CHAVE_ACESSO").val(),
                itemNf: itemNF
            },
            error: function () {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao consultar o pedido",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                HideLoading();
                if (oReturn.result == "Ok") {
                    $("#divConsultarInbound").modal();
                    $("#divInboundDeliveryBody").empty();
                    $("#divInboundDeliveryBody").append(oReturn.data);

                    if (refreshData == true) {
                        $.smallBox({
                            title: "VNF NELES",
                            content: "Inbound delivery selecionada. Associe novamente a nota fiscal.",
                            color: "#739E73",
                            icon: "fa fa-check fadeInLeft animated",
                            timeout: 4000
                        });
                    }
                }
                else {
                    $.smallBox({
                        title: "Consultar Inbound",
                        content: oReturn.data,
                        color: "#C46A69",
                        iconSmall: "fa fa-times fa-2x fadeInRight animated",
                        timeout: 4000
                    });
                }
            }
        });
    }
}


function SelecionarInbound(id, inboundDelivery, inboundDeliveryItem) {
    $("#divConsultarInbound").modal("hide");
    ShowLoading("Aguarde", "Verificando informações da inbound delivery selecionada...");
    var hdfPedido = $("#hdfPedido").val();
    var itemNF = $("#hdfId").val();

    if (hdfPedido == "" || itemNF == "") {
        $.SmartMessageBox({
            title: "Consultar inbound",
            content: "Selecione uma linha com pedido e item para efetuar a consulta.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
            $("#divConsultarInbound").modal("show");
            return;
        });
    } else {
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Associacao/SelecionarInbound",
            dataType: 'json',
            data: {
                nfeid: $("#VNF_CHAVE_ACESSO").val(),
                itemNf: itemNF,
                inboundDelivery: inboundDelivery,
                inboundDeliveryItem: inboundDeliveryItem
            },
            error: function () {
                HideLoading();
                $("#divConsultarInbound").modal("show");
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao selecionar a inbound",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                if (oReturn.data == "") {
                    ConsultarInbound(true);
                } else {
                    $.SmartMessageBox({
                        title: "Inbound Delivery",
                        content: oReturn.data,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                        $("#divConsultarInbound").modal("show");
                        return;
                    });
                }
            }
        });
    }
}

function CarregarNotasInbound(indice) {
    if ($("#detalheItem_" + indice).css("display") != "none") {
        $(".collapse").hide();
        $("#btnShowNfsInbound_" + indice).removeClass("fa-chevron-up");
        $("#btnShowNfsInbound_" + indice).addClass("fa-chevron-down");
    } else {
        $(".collapse").hide();
        $(".fa-chevron-up").addClass("fa-chevron-down");
        $(".fa-chevron-up").removeClass("fa-chevron-up");

        $("#detalheItem_" + indice).show();
        $("#btnShowNfsInbound_" + indice).removeClass("fa-chevron-down");
        $("#btnShowNfsInbound_" + indice).addClass("fa-chevron-up");
    }
}

$(function () {
    $('#btAddNotaRef').on('click', function () {
        AddRowRefNF("","");
    });
});
function AddRowRefNF(pStrNUMERO_REFNF, pStrSERIE_REFNF) {
    $('#gridNotasRef').append('<tr>' +
               '<td class="numerorefnf">  <span class="display-mode"> </span> <input type="text" id="txtNumeroNFRef_2" class="input-editable" value="' + pStrNUMERO_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="6"></td>' +
               '<td class="serierefnf">  <span class="display-mode"> </span> <input type="text" id="txtSerieRef_2" class="input-editable" value="' + pStrSERIE_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="6"></td>' +
               '<td><a id="btDeleteRefNF" onclick=\"ExcluirRefNF()\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a></td>' +
               '</tr>');
}

function LoadRowsRefNF(pItenNF) {
    ItemRefNF = pItenNF;
    ClearRefNFTable();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/GetRefNF",
        dataType: 'json',
        data: {
            pStrNFEID: $("#hfIdNfe").val(), 
            pStrItemNF: pItenNF,
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.sucesso) {
                var ArrayRefNf = oReturn.RefNFList;
                for (index = 0; index < ArrayRefNf.length; ++index) {
                    AddRowRefNF(ArrayRefNf[index].NUMERO_REFNF, ArrayRefNf[index].SERIE_REFNF);
                }
               
            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}
function ClearRefNFTable() {
    $('#gridNotasRef').empty();
}

$(function () {
    $('#btSalvarRefNF').on('click', function () {
        SalvarRefNFTable();
    });
});

function SalvarRefNFTable() {

    //Lendo tabela de notas referenciasdas e criando um array de objetos
    var pArrayRefNf = new Array();
    $('#gridNotasRef tr').each(function (i, row) {
        //Here I need to loop the tr again ( i.e. row) 
        var pObjRefNF = new Object();
        pObjRefNF.NFEID = $("#hfIdNfe").val();
        pObjRefNF.ITEM_NF = ItemRefNF;
        $("input", row).each(function (i, sr) {
            //here  I want to get all the textbox values
            if (i == 0) {
                if ($.trim($(sr).eq(0).val()) == "") {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Número da nota em branco não permitido. ",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                    return;
                }
                pObjRefNF.NUMERO_REFNF = $.trim($(sr).eq(0).val());
            }
            else {
                if ($.trim($(sr).eq(0).val()) == "") {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Número da série em branco não permitido. ",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                    return;
                }
                pObjRefNF.SERIE_REFNF = $.trim($(sr).eq(0).val());
            }
        });
        pArrayRefNf.push(pObjRefNF);
    });

    if (pArrayRefNf.length == 0) {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Nenhuma nota de referência para salvar.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    //Salvando dados das notas referênciadas
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Associacao/SalvarRefNF",
        contentType: 'application/json',
        dataType: 'html',
        data: JSON.stringify(pArrayRefNf),
        error: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (JSON.parse(oReturn).sucesso) {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Referência(s) salva(s) com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}

$(function () {
    $('#btAddNotaRefSubContratacao').on('click', function () {
        AddRowRefNFSubContratacao("", "", "");
    });
});
function AddRowRefNFSubContratacao(pStrNUMERO_REFNF, pStrSERIE_REFNF, pStrITEM_REFNF) {
    $('#gridNotasRefSubContratacao').append('<tr>' +
               '<td class="col-md-3 no-padding">  <span class="display-mode"> </span> <input type="text" id="txtNumeroNFRef_3" class="input-editable" value="' + pStrNUMERO_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="6"></td>' +
               '<td class="col-md-2 no-padding">  <span class="display-mode"> </span> <input type="text" id="txtSerieRef_3" class="input-editable" value="' + pStrSERIE_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="3"></td>' +
               '<td class="col-md-2 no-padding">  <span class="display-mode"> </span> <input type="text" id="txtItemRef_3" class="input-editable" value="' + pStrITEM_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="3"></td>' +
               '<td class="col-md-5 no-padding"><a id="btDeleteRefNFSubContratacao" onclick=\"ExcluirRefNFSubContratacao()\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a></td>' +
               '</tr>');
}

function LoadRowsRefNFSubContratacao(pItenNF) {
    ItemRefNF = pItenNF;
    ClearRefNFSubContratacaoTable();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/GetRefNFSubContratacao",
        dataType: 'json',
        data: {
            pStrNFEID: $("#hfIdNfe").val(),
            pStrItemNF: pItenNF,
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.sucesso) {
                var ArrayRefNf = oReturn.RefNFList;
                for (index = 0; index < ArrayRefNf.length; ++index) {
                    AddRowRefNFSubContratacao(ArrayRefNf[index].NUMERO_REFNF, ArrayRefNf[index].SERIE_REFNF, ArrayRefNf[index].ITEM_REFNF);
                }

            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}

function ClearRefNFComplementarTable() {
    $('#gridNotasRefNFComplementar').empty();
}

$(function () {
    $('#btSalvarRefNFComplementar').on('click', function () {
        SalvarRefNFComplementarTable();
    });
});

function ClearRefNFSubContratacaoTable() {
    $('#gridNotasRefSubContratacao').empty();
    $('#gridNotasRefSubContratacao').append('<thead> <tr> <th class="col-md-3 no-padding">Nota Ref.</th> <th class="col-md-2 no-padding">Setie Ref.</th> <th class="col-md-2 no-padding">Item Ref.</th> </tr> </thead>');
}

$(function () {
    $('#btSalvarRefNFSubContratacao').on('click', function () {
        SalvarRefNFSubContratacaoTable();
    });
});

function SalvarRefNFSubContratacaoTable() {

    //Lendo tabela de notas referenciasdas e criando um array de objetos
    var pArrayRefNf = new Array();
    var msgerro = "";
    var vIntRowCount = 0;
    $('#gridNotasRefSubContratacao tr').each(function (i, row) {
        //Here I need to loop the tr again ( i.e. row) 
        var pObjRefNF = new Object();
        pObjRefNF.NFEID = $("#hfIdNfe").val();
        pObjRefNF.ITEM_NF = ItemRefNF;
        $("input", row).each(function (i, sr) {
            //here  I want to get all the textbox values
            if (i == 0) {
                if ($.trim($(sr).eq(0).val()) == "") {
                   msgerro = "Número da nota em branco não permitido. ";
                   return false;
                }
                pObjRefNF.NUMERO_REFNF = $.trim($(sr).eq(0).val());
            }
            else if (i == 1) {
                if ($.trim($(sr).eq(0).val()) == "") {
                    msgerro = "Número da série em branco não permitido. ";
                    return false;
                }
                pObjRefNF.SERIE_REFNF = $.trim($(sr).eq(0).val());
            }
            else {
                if ($.trim($(sr).eq(0).val()) == "") {
                    msgerro = "Número do item da nota de referência em branco não permitido. ";
                    return false;
                }
                pObjRefNF.ITEM_REFNF = $.trim($(sr).eq(0).val());
            }
            if (msgerro != "") {
                return false;
            }
        });
        if (msgerro == "") {
            if (vIntRowCount > 0) {
                pArrayRefNf.push(pObjRefNF);
            }
        }
        else {
            return false;
        }
        vIntRowCount++;
    });

    if (pArrayRefNf.length == 0) {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Nenhuma nota de referência para salvar. Erros: " + msgerro,
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    //Salvando dados das notas referênciadas
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Associacao/SalvarRefNFSubContratacao",
        contentType: 'application/json',
        dataType: 'html',
        data: JSON.stringify(pArrayRefNf),
        error: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (JSON.parse(oReturn).sucesso) {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Referência(s) salva(s) com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}

$(function () {
    $('#btAddNotaRefNFComplementar').on('click', function () {
        AddRowRefNFComplementar("","");
    });
});

function AddRowRefNFComplementar(pStrNUMERO_REFNF, pStrSERIE_REFNF) {
    $('#gridNotasRefNFComplementar').append('<tr>' +
               '<td class="numerorefnf">  <span class="display-mode"> </span> <input type="text" id="txtNumeroNFRef_2" class="input-editable" value="' + pStrNUMERO_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="6"></td>' +
               '<td class="serierefnf">  <span class="display-mode"> </span> <input type="text" id="txtSerieRef_2" class="input-editable" value="' + pStrSERIE_REFNF + '" onkeypress="return onlyInt(event)" onchange="" maxlength="6"></td>' +
               '<td><a id="btDeleteRefNF" onclick=\"ExcluirRefNFComplementar()\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a></td>' +
               '</tr>');
}

function LoadRowsRefNFNotaComplementar(pItenNF) {
    ItemRefNF = pItenNF;
    ClearRefNFComplementarTable();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/GetRefNFNotaComplemenetar",
        dataType: 'json',
        data: {
            pStrNFEID: $("#hfIdNfe").val(),
            pStrItemNF: pItenNF,
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.sucesso) {
                var ArrayRefNf = oReturn.RefNFList;
                for (index = 0; index < ArrayRefNf.length; ++index) {
                    AddRowRefNFComplementar(ArrayRefNf[index].NUMERO_REFNF, ArrayRefNf[index].SERIE_REFNF);
                }

            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao carregar notas de remessa para referência. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}

function SalvarRefNFComplementarTable() {

    //Lendo tabela de notas referenciasdas e criando um array de objetos
    var pArrayRefNf = new Array();
    $('#gridNotasRefNFComplementar tr').each(function (i, row) {
        //Here I need to loop the tr again ( i.e. row) 
        var pObjRefNF = new Object();
        pObjRefNF.NFEID = $("#hfIdNfe").val();
        pObjRefNF.ITEM_NF = ItemRefNF;
        $("input", row).each(function (i, sr) {
            //here  I want to get all the textbox values
            if (i == 0) {
                if ($.trim($(sr).eq(0).val()) == "") {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Número da nota em branco não permitido. ",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                    return;
                }
                pObjRefNF.NUMERO_REFNF = $.trim($(sr).eq(0).val());
            }
            else {
                if ($.trim($(sr).eq(0).val()) == "") {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Número da série em branco não permitido. ",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                    return;
                }
                pObjRefNF.SERIE_REFNF = $.trim($(sr).eq(0).val());
            }
        });
        pArrayRefNf.push(pObjRefNF);
    });

    if (pArrayRefNf.length == 0) {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Nenhuma nota de referência para salvar.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    //Salvando dados das notas referênciadas
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Associacao/SalvarRefNFCompelmentar",
        contentType: 'application/json',
        dataType: 'html',
        data: JSON.stringify(pArrayRefNf),
        error: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (JSON.parse(oReturn).sucesso) {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Referência(s) salva(s) com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao salvar referência(s)." + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}



function ExcluirRefNF() {
    $("#btDeleteRefNF").parents('tr').first().remove();
   
}

function ExcluirRefNFComplementar() {
    $("#btDeleteRefNFComplementar").parents('tr').first().remove();
   
}

function ExcluirRefNFSubContratacao() {
    $("#btDeleteRefNFSubContratacao").parents('tr').first().remove();
}

