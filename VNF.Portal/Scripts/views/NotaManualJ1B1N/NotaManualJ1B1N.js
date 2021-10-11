var jsonObj;

var lastSelItemGrid;
var PodeModificar;
var CriadoManualmente;

$(document).ready(function () {

    PodeModificar = $("#hdfPodeModificar").val();
    CriadoManualmente = $("#hdfCriadoManualmente").val();
    //alert(PodeModificar);
    //alert(CriadoManualmente);
    //Ocultar labe de doc num caso o documento ainda não esteja gerado no sap
    if ($("#hdfSalvo").val() != "2" && PodeModificar == "True") {
        $("#SAPDocNum").hide();
        EnableControls();
    }
    else {
        DisableControls();
    }

    //Ocultar menu lateral
    $('#hide-menu > span > a').trigger("click");
    $('span.minifyme').trigger("click");
    $('#hide-menu > span > a').trigger("click");

    if ($("#hdfModelo").val() == "55") {
        var mydata = $("#itemdata").val();
        var itemtax = $("#itemtax").val();
        var CFOP_List = $("#hdfCFOP_List").val();

        jsonObj = $.parseJSON(itemtax);

        var subgrid_table_id;
        $("#jQGridDemo").jqGrid({
            datatype: "jsonstring",
            //colNames: ['Item', 'Tipo', 'Material', 'Descrição', 'Centro', 'Quantidade', 'Preço', 'Total', 'CFOP', 'NCM', 'UN', 'Doc Ref Orig.', 'Item Ref NF', 'Orig', 'Utilização'],
            colModel: [{ label: 'Item', name: 'ITEM_NF', key: true, width: 5, align: 'left', sortable: false },
                       {
                           label: 'Tipo', name: 'ITEM_TYPE_PARA', width: 5, align: 'left', editable: true, editoptions: {
                               maxlength: "2"
                           }
                       },
                       {
                           label: 'Material', name: 'COD_MATERIAL_PARA', width: 25, align: 'left', editable: true, sortable: false, editoptions: {
                               maxlength: "18"
                           }
                       },
                       { label: 'Descrição', name: 'MAT_DESCRICAO_MAKTX', width: 40, align: 'left', editable: false, sortable: false },
                       {
                           label: 'Centro', name: 'PLANTA_PARA', width: 6, align: 'left', editable: true, sortable: false, editoptions: {
                               maxlength: "4"
                           }
                       },
                       {
                           label: 'Quantidade', name: 'QUANTIDADE_PARA', width: 15, align: 'left', sortable: false, editable: true,
                           editoptions: {
                               maxlength: "8"
                           }
                       },
                       { label: 'Preço', name: 'VALOR_UNIT_PARA', width: 10, align: 'right', editable: true, sortable: false },
                       { label: 'Total', name: 'VALOR_TOT_PARA', width: 10, align: 'right', editable: false, sortable: false },
                       {
                           label: 'CFOP', name: 'CFOP_PARA', width: 8, align: 'left', editable: true, sortable: false, edittype: "select", text: "CFOP",
                           editoptions: {
                               value: CFOP_List
                           }
                       },
{
                           label: 'NCM', name: 'NCM_PARA', width: 10, align: 'left', sortable: false, editable: true, formatter: NCMFomatter, editoptions: {
                               maxlength: "16"
                           }
                       },
                       { label: 'Unidade', name: 'UNIDADE_PARA', width: 5, align: 'left', editable: false, sortable: false },
                       {
                           label: 'Documento referência original', name: 'DOCNUM_REF', width: 10, align: 'left', sortable: false, editable: true, editoptions: {
                               maxlength: "9"
                           }
                       },
                       { label: 'Item de referência da NF', name: 'ITEM_DOCNUM_REF', width: 10, align: 'left', sortable: false, editable: true },
                       {
                           label: 'Origem', name: 'MAT_ORIG_PARA', width: 3, align: 'left', editable: true, sortable: false, edittype: "select", text: "Origem",
                           editoptions: {
                               value: "0:0;1:1;2:2;3:3"
                           }
                       },
                       { label: 'Utilização', name: 'MAT_USO', width: 6, align: 'left', editable: true, hidden: true },
                       { label: 'Grupo', name: 'MAT_GRUPO_MATKL', width: 5, align: 'left', editable: false, hidden: true }],


            loadonce: true,
            rowNum: 900,
            height: "290",
            viewrecords: true,
            autowidth: true,
            gridview: true,
            cellEdit: true,
            cellsubmit: 'clientArray',
            datastr: mydata,
            repeatitems: false,
            afterSaveCell: GetCheckItemFields,
            subGrid: true,
            scrollerbar: true,
            ////onSelectRow: function (idtax) {
            ////    //if (id && id !== lastSel) {
            ////    //$("#" + childGridID).jqGrid('restoreRow', lastSelTaxGgrid);
            ////    //$("#" + childGridID).jqGrid('editRow', idtax, true, null, null, 'clientArray');
            ////    lastSelItemGrid = idtax;
            ////    //}
            ////},
            subGridOptions: {
                reloadOnExpand: false,
            },
            subGridRowExpanded: function (parentRowID, parentRowKey) {
                //subGridRowExpanded: function (subgrid_id, row_id) {
                // we pass two parameters
                // subgrid_id is a id of the div tag created within a table
                // the row_id is the id of the row
                // If we want to pass additional parameters to the url we can use
                // the method getRowData(row_id) - which returns associative array in type name-value
                // here we can easy construct the following
                // create unique table and pager

                //teste
                var childGridID = parentRowID + "_table";
                //var childGridPagerID = parentRowID + "_pager";

                // send the parent row primary key to the server so that we know which grid to show
                var childGridURL = parentRowKey + ".json";
                // add a table and pager HTML elements to the parent grid row - we will render the child grid here
                $('#' + parentRowID).append('<table id=' + childGridID + '></table>');//<div id=' + childGridPagerID + '></div>');

                $("#" + childGridID).jqGrid({
                    //url: childGridURL,
                    //mtype: "GET",
                    datatype: "jsonstring",
                    //page: 1,
                    colModel: [
                      { label: "Imposto", name: "Imposto", width: 70, sortable: false },
                      { label: "Tipo", name: "TIPO_PARA", width: 70, sortable: false, editable: true },
                      { label: "Lei", name: "LEI_PARA", width: 70, align: "right", editable: true, sortable: false },
                      { label: "CST", name: "CST", width: 70, align: "right", sortable: false },
                      { label: "Base de cálculo", name: "BASE_CALC_PARA", width: 70, align: "right", editable: true, sortable: false },
                      { label: "Alíquota", name: "ALIQUOTA_PARA", width: 70, align: "right", editable: true, sortable: false },
                      { label: "Valor", name: "VALOR_PARA", width: 90, align: "right", editable: true, sortable: false },
                      { label: "Base Exclus.", name: "BASE_EXCL_PARA", width: 70, align: "right", editable: true, sortable: false },
                      { label: "Base Outras", name: "BASE_OUTR_PARA", width: 70, align: "right", editable: true, sortable: false },
                      { label: "ItemNF", name: "ITEM_NF", width: 90, align: "right", hidden: true }
                    ],
                    loadonce: true,
                    cellsubmit: 'clientArray',
                    autowidth: true,
                    shrinkToFit: false,
                    height: '100%',
                    datastr: JSON.stringify(jsonObj[parentRowKey - 1]),
                    cellEdit: true,
                });
            },
        });
    }
    
    //Carregtando logs do documento
    if ($("#hdfLog").val() == "") {
        $("#divLog").append("<span>não existe informação</span>");
    } else {
        $("#divLog").append($("#hdfLog").val());
    }
    //Carregando anexos do documento
    CarregaAnexos();
})

function displayMessageError() {

    $.SmartMessageBox({
        title: "Ocorreu um erro inesperado no sistema. " ,
        content: $("#MsgError").val(),
        buttons: '[Ok]'
    }, function (ButtonPress, Value) {
        HideLoading();
    });

}

function GetXml() {
    IdNfe = $("#hdfNFEID").val();
    var urlXml = url + "Compras/GetXml/" + IdNfe;

    window.open(urlXml, '_blank');
}

function GetDanfe() {

    var GetDanteUrl = url + 'Compras/DownloadDanfe?pIdNfe=' + $("#hdfNFEID").val();
    window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    
}


function SalvarJ1B1N() {

    //Verificando se o documento já não foi enviado para o SAP
    if ($("#hdfSalvo").val() == "2") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Documento já enviado para o SAP, não é permitido salvar alterações.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    //Formatando dados de cabeçalho para gravar 
    var pObjNF = new Object();

    pObjNF.NFEID = $("#hdfNFEID").val();
    pObjNF.NFTYPE_PARA = $("#CategoriaNF").val();
    pObjNF.PARTNER_FUNCTION_PARA = $("#FuncaoParceiro").val();
    pObjNF.PARTNER_ID = $("#IdParceiro").val();
    pObjNF.OBSERVACAO = $("#txtObservacao").val();
    pObjNF.DOCNUM_REF= $.trim($("#txtDocNumRef").val());

    //Atribuindo itens
    //Removendo o modo de edição do grid para pode coletar os dados
    $("#jQGridDemo").jqGrid('saveRow', lastSelItemGrid, false, 'clientArray');
    pObjNF.TBJ1B1N_ITEM_NFE = $("#jQGridDemo").getRowData();

    //Atribuindo impostos
    var itemrows = jQuery("#jQGridDemo").getDataIDs();
    var itemtaxrows;
    for (itemf = 1; itemf < itemrows.length + 1; itemf++) {
        
        itemtaxrows = $("#jQGridDemo_" + itemf + "_table").getDataIDs();
        if (itemtaxrows.length > 0) {
            //Removendo o modo de edição do grid para pode coletar os dados.
            $("#jQGridDemo_" + itemf + "_table").jqGrid('editCell', 0, 0, false);
            //Formantando campos numéricos para decimal simples (removendo pontos de centena)
            for (var i = 1; i < itemtaxrows.length + 1; i++) {
                $("#jQGridDemo_" +itemf + "_table").jqGrid("setCell", i, "VALOR_PARA", $("#jQGridDemo_" +itemf + "_table").jqGrid('getCell', i, 'VALOR_PARA').replace(".", ""));
                $("#jQGridDemo_" +itemf + "_table").jqGrid("setCell", i, "BASE_EXCL_PARA", $("#jQGridDemo_" +itemf + "_table").jqGrid('getCell', i, 'BASE_EXCL_PARA').replace(".", ""));
                $("#jQGridDemo_" +itemf + "_table").jqGrid("setCell", i, "BASE_OUTR_PARA", $("#jQGridDemo_" +itemf + "_table").jqGrid('getCell', i, 'BASE_OUTR_PARA').replace(".", ""));
            }
            //Coletando os dados
            pObjNF.TBJ1B1N_ITEM_NFE[itemf - 1].TBJ1B1N_ITEMTAX_NFE = $("#jQGridDemo_" + itemf + "_table").getRowData();
        }
    }

    ShowLoading("Aguarde", "Salvando documento...");

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "NotaManual_J1B1N/SalvarJ1B1N",
        contentType: 'application/json',
        dataType: 'html',
        data: JSON.stringify(pObjNF),
        error: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao salvar dados da nota. " + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (JSON.parse(oReturn).sucesso) {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Documento salvo com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao salvar dados da nota." + JSON.parse(oReturn).MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }


        }
    });
}
function GetCheckItemFields(rowid, celname, value, iRow, iCol) {
    //Verificando se o documento já não foi enviado para o SAP
    if ($("#hdfSalvo").val() == "2") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Documento já enviado para o SAP, não é permitido fazer alterações.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    UnSaveDocument();
    
    if (celname == "COD_MATERIAL_PARA") {

        GetMaterialDataFromSap(rowid, celname, value, iRow, iCol);
    }
    else if (celname == "DOCNUM_REF") {
        CheckDOCNUMExists(rowid, celname, value, iRow, iCol);
    }
    else if (celname == "VALOR_UNIT_PARA" || celname == "QUANTIDADE_PARA") {

        var valorTotalPara = Math.round(($('#jQGridDemo').jqGrid('getCell', rowid, 'QUANTIDADE_PARA').replace(".","").replace(",",".")) * ($('#jQGridDemo').jqGrid('getCell', rowid, 'VALOR_UNIT_PARA').replace(".","").replace(",",".") ) * 100) / 100;

        var formatter = new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL',
            minimumFractionDigits: 2, 
        });

        $("#jQGridDemo").jqGrid("setCell", rowid, "VALOR_TOT_PARA", formatter.format(valorTotalPara).replace("R$", ""));
    }
    else if (celname == "CFOP_PARA") {
        $("#jQGridDemo").jqGrid("setCell", rowid, "CFOP_PARA", $('#jQGridDemo').jqGrid('getCell', rowid, 'CFOP_PARA').substring(0, 6));
    }
}
function UnSaveDocument() {
    //Verificando se o documento já não foi enviado para o SAP
    if ($("#hdfSalvo").val() == "2") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Documento já enviado para o SAP, não é permitido salvar alterações.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "NotaManual_J1B1N/UnSaveDocument",
        dataType: 'json',
        data: {
            pStrNFEID: $("#hdfNFEID").val()
        }
        
        });
    

}

function GerarJ1B1N() {

    //Verificando se o documento já não foi enviado para o SAP
    if ($("#hdfSalvo").val() == "2") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Documento já enviado para o SAP, não é permitido reenviar.",
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
        });
        return;
    }
    


    ShowLoading("Aguarde", "Enviando documento...");
        $.ajax({
            async: true,
            cache: false,
            type: 'post',
            url: url + "NotaManual_J1B1N/GerarJ1B1N",
            dataType: 'json',
            data: {
                pStrNFEID: $("#hdfNFEID").val()
            },
            error: function () {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao gerar nota manual. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function (oReturn) {
                if (oReturn.sucesso) {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Nota manual gerada com sucesso. SAP docnum: " + oReturn.Docnum,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                        window.location.replace(document.referrer);
                    });
                }
                else {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Erro ao gerar nota manual. " + oReturn.MensagemErro,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                }


            }
        });
   
}


function GetMaterialDataFromSap(rowid, celname, value, iRow, iCol) {

    var rowData = $('#jQGridDemo').jqGrid('getRowData', rowid);

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "NotaManual_J1B1N/GetMaterialDataFromSap",
        dataType: 'json',
        data: {
            pStrItemNF: rowData.ITEM_NF,
            pStrCodmaterial: value,
            pStrPlanta: rowData.PLANTA_PARA,
            pStrDescricao: rowData.MAT_DESCRICAO_MAKTX
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao carregar dados do material. " + oReturn.MensagemErro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.sucesso) {
                if (oReturn.DescricaoMaterial != "") {
                    rowData.MAT_DESCRICAO_MAKTX = oReturn.DescricaoMaterial;
                    rowData.MAT_ORIG_PARA = oReturn.MaterialOrigem;
                    rowData.MAT_USO = oReturn.MaterialUtilizacao;
                    rowData.UNIDADE_PARA = oReturn.MaterialUnidadeMed;
                    rowData.MAT_GRUPO_MATKL = oReturn.MaterialGrupo;
                    $('#jQGridDemo').jqGrid('setRowData', rowid, rowData);
                }
                else {
                    //rowData.MAT_DESCRICAO_MAKTX = "";
                    rowData.MAT_ORIG_PARA = "";
                    rowData.MAT_USO = "";
                    rowData.UNIDADE_PARA = "";
                    rowData.MAT_GRUPO_MARKL = "";
                    $('#jQGridDemo').jqGrid('setRowData', rowid, rowData);
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Material '" + value + "' com alguma inconsistência ou não encontrado no SAP.",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                }

            }
            else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao carregar dados do material. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }

        }
    });
}

function CheckDOCNUMExists(rowid, celname, value, iRow, iCol) {
    //Verificando se o document number digitado existe no SAP
    
        var rowData = $('#jQGridDemo').jqGrid('getRowData', rowid);

        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "NotaManual_J1B1N/CheckDOCNUMExists",
            dataType: 'json',
            data: {
                pStrDocnum: rowData.DOCNUM_REF
            },
            error: function (oReturn) {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao consultar DOCNUM. " + oReturn.MensagemErro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function (oReturn) {
                if (oReturn.sucesso) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Docnum " + oReturn.MensagemRetorno + " localizado com sucesso! ",
                        color: "#739E73",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 2000
                    });
                }
                else {
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Erro ao consultar DOCNUM " + oReturn.MensagemRetorno + " , documento não localizado. " ,
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                }


            }
        });
}

function Estornar() {

  
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo do estorno dos dados de integração no VNF",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
            return 0;
        }
        if (ButtonPress == "Confirmar") {
            var Value1 = Value.toUpperCase();
            if (Value1 == "") {
                $.SmartMessageBox({
                    title: "Erros",
                    content: "Informar a observação",
                    buttons: '[Fechar]'
                }, function (ButtonPress, Value) {
                    if (ButtonPress == "Fechar") {
                        //$("#divDetalhe").modal();
                    }
                });
            } else {
                EstornarVNF(Value1);
            }
        }
    });
};

function EstornarVNF(observacao) {
    hdfId = $("#hdfNFEID").val();

     ShowLoading("Aguarde", "Estornando documento...");

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/EstornarVNF",
        dataType: 'json',
        data: {
            NfeId: hdfId,
            observacao: observacao
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao estornar documento.",
                buttons: '[Fechar]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Documento estornado com sucesso",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
                window.location.replace(document.referrer);
            });
        }
    });
    
}



function NCMFomatter(cellvalue, options, rowObject) {
    // do something here
    var ncm = cellvalue.replace(/\./g, '');
    new_format_value = ncm.substring(0, 4) + "." + ncm.substring(6, 4) + "." + ncm.substring(8, 6);
    return new_format_value
}


$(document).on('focusout', '[role="gridcell"] *', function () {
    $("#jQGridDemo").jqGrid('editCell', 0, 0, false);
});

function Recusar() {
    //$("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo da recusa",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
           // $("#divDetalhe").modal("show");
            return 0;
        }
        if (ButtonPress == "Confirmar") {
            var Value1 = Value.toUpperCase();
            if (Value1 == "") {
                $.SmartMessageBox({
                    title: "Erros",
                    content: "Informar a observação",
                    buttons: '[Fechar]'
                }, function (ButtonPress, Value) {
                    if (ButtonPress == "Fechar") {
                        //$("#divDetalhe").modal();
                    }
                });
            } else {
                confirmarRecusa("R", Value1);
               // $("#divDetalhe").modal();
            }
        }
    });
};

function confirmarRecusa(tipo, observacao) {
    hdfId = $("#hdfNFEID").val();

    if (tipo == "R") {
        ShowLoading("Aguarde", "Realizando recusa do documento...");
    }
    else {
        ShowLoading("Aguarde", "Desfazendo recusa do documento...");
    }
    
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/RecusaVNFE",
        dataType: 'json',
        data: {
            NfeId: hdfId,
            tipo: tipo,
            observacao: observacao
        },
        error: function () {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro no processaomento.",
                buttons: '[Fechar]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Processamento realizado com sucesso.",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
            window.location.replace(document.referrer);
        }
    });
    return true;
}

function DesfazerRecusa() {
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo do cancelamento da recusa",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
            return 0;
        }

        Value1 = Value.toUpperCase();
        if (Value1 == "") {
            $.SmartMessageBox({
                title: "Erro",
                content: "Informe a observação",
                buttons: '[Fechar]'
            });
        }
        else
            confirmarRecusa("D", Value1);

    });
};

function RegistroManual() {
    $("#MsgBoxBack").show();

    var IdNfe = $("#hdfNFEID").val();
    ShowLoading("Aguarde", "atualizando informações no sistema")
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/RegistroManual",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
            $.SmartMessageBox({
                title: "Ocorreu uma falha no processamento",
                content: "repita novamente o processo ou envie este erro para o administrador",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            if (oReturn.data == "") {
                $.SmartMessageBox({
                    title: "Registro manual",
                    content: "Os dados foram atualizados com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.replace(document.referrer);
                });
            } else {
                $.SmartMessageBox({
                    title: "Não foi possível atualizar os dados",
                    content: oReturn.data,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.replace(document.referrer);
                });
            }
        }
    });
}

function UploadAnexo() {
    hfIdNfe = $("#hdfNFEID").val();

    if (typeof FormData == "undefined") {
        var formdata = [];
    } else {
        var formdata = new FormData();
    }


    var fileInput = document.getElementById("FileInputfileAnexo");
    if (fileInput.files != null) {
        for (i = 0; i < fileInput.files.length; i++) {
            formdata.append(fileInput.files[i].name, fileInput.files[i]);
        }
    }

    formdata.append("NFEID", hfIdNfe);
    var params;
    if (typeof FormData == "undefined") {
        params = JSON.stringify(formdata);
    } else {
        params = formdata;
    }

    $('html, body').animate({ scrollTop: 0 }, 'slow');

    var xhr = new XMLHttpRequest();
    xhr.open("POST", url + "Ocorrencias" + "/" + "UploadAnexo", true);
    xhr.setRequestHeader("Content-length", params.length);
    xhr.setRequestHeader("Connection", "close");
    xhr.send(params);

    xhr.onload = function () {
        window.scrollTo(0, 0);
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {

                if (xhr.responseText != "") {
                    var postReturn = JSON.parse(xhr.responseText);
                }

                if (postReturn != undefined && postReturn.result == "Error") {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                } else {
                    CarregaAnexos();
                }
            }
            else {
                $.smallBox({
                    title: "Erros",
                    content: "Não foi possível completar sua solicitação!",
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            }
        }
    }
}

function CarregaAnexos() {
    var NFEID = $("#hdfNFEID").val();
    $("#lblQtdAnexos").text('');
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/GetOcorrenciaAnexos",
        dataType: 'json',
        data: { NFEID: NFEID },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#dtAnexos tbody").empty();
            $("#dtAnexos tbody").append(oReturn.html);
            if (oReturn.qtdOcorr > 0) {
                $("#lblQtdAnexos").append(oReturn.qtdOcorr);
                $("#lblQtdAnexos").show();
            } else {
                $("#lblQtdAnexos").hide();
            }
        }
    });
}

function ExcluirdAnexo(CodLog) {

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/ExcluirAnexoOcorrencia",
        dataType: 'text',
        data: {
            id: CodLog
        },
        error: function () {
            $.smallBox({
                title: "VNF NELES",
                content: "Ocorreu um erro ao tentar excluir o anexo.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 2000
            });

        },
        success: function () {
            CarregaAnexos();
            $.smallBox({
                title: "VNF NELES",
                content: "Anexo excluido com sucesso.",
                color: "#739E73",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 2000
            });
        }
    });
}

function EnableControls() {

    $("#btnDesfazer").prop("disabled", false);
    $("#btnRecusar").prop("disabled", false);
    //$("#btnDesfazer").prop("disabled", false);
  //  $("#btnDesfazer").removeClass("disabled");
    if (!CriadoManualmente == "True") {
        $("#btnRegistroManual").prop("disabled", false);
    }
    $("#btnGerarJ1B1N").prop("disabled", false);
    $("#btnSalvaJ1B1N").prop("disabled", false);
    
    

}

function DisableControls() {

    $("#btnDesfazer").prop("disabled", true);
    $("#btnRecusar").prop("disabled", true);
    $("#btnDesfazer").prop("disabled", true);
    if (CriadoManualmente == "True" && PodeModificar == "True") {
        $("#btnRegistroManual").prop("disabled", false);
    }
    else {
        $("#btnRegistroManual").prop("disabled", true);
    }
    $("#btnGerarJ1B1N").prop("disabled", true);
    $("#btnSalvaJ1B1N").prop("disabled", true);
    

}