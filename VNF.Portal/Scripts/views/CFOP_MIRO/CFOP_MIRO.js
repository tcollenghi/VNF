/// <reference path="../../libs/jquery-2.1.1.min.js" />

$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

function LoadData() {
    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + "CFOP_MIRO/GetData",
        dataType: 'json',
        data: {
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {

            if (oReturn.data.length == 0) {

                $("#dttTabela tfoot").hide();
                ;
            } else {
                $("#dttTabela tbody").append(oReturn.data);
            }
        }
    });
}

function Novo() {
    $("#idItem").text("Novo");
    $("#txtCFOP_XML").val('');
    $("#txtCFOP_SAP").val('');
    $("#txtCFOP_ESCRITURAR").val('');
    $("#hdfCFOPXML").val('');
    $("#hdfCFOPSAP").val('');
    $("#hdfCFOPESCRITURAR").val('');
    $("#txtCFOP_XML").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'CFOP_MIRO/Index';
}

function SalvarCFOPMiro() {
    txtcfopxml = $("#txtCFOP_XML").val();
    txtcfopsap = $("#txtCFOP_SAP").val();
    txtcfopescriturar = $("#txtCFOP_ESCRITURAR").val();
    txtcfopescriturarantigo = $("#hdfCFOPESCRITURAR").val();


    var erros = "";

    if (txtcfopxml == "" || txtcfopxml == 0) {
        erros += "<p>Informar o CFOP XML</p>"
    }

    if (txtcfopsap == "" || txtcfopsap == 0) {
        erros += "<p>Informar o CFOP SAP</p>"
    }


    if (txtcfopescriturar == "" || txtcfopescriturar == 0) {
        erros += "<p>Informar o CFOP Escriturar</p>"
    }
    
    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 1000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#idItem").val();
    if (Editar == 'EDITAR') {
        UrlParametro = url + "CFOP_MIRO/Editar";
        pData = {
            pCFOP_XML: txtcfopxml,
            pCFOP_SAP: txtcfopsap,
            PCFOP_ESCRITURAR: txtcfopescriturar
        }
    }
    else {
        UrlParametro = url + "CFOP_MIRO/Salvar";
        pData = {
            pCFOP_XML: txtcfopxml,
            pCFOP_SAP: txtcfopsap,
            PCFOP_ESCRITURAR: txtcfopescriturar
        }
    }

  //  Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar o processo",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {
            $("#txtCFOP_XML").val('');
            $("#txtCFOP_SAP").val('');
            $("#txtCFOP_ESCRITURAR").val('');
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Processo realizada com sucesso",
                timeout: 1000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
            Atualizar();
        }
    });
}

function Editar(pEditar) {
    $("#idItem").text("Editar");

    pCFOP_XML = $("#hdfCFOPXML").val();
    pCFOP_SAP = $("#hdfCFOPSAP").val();
    PCFOP_ESCRITURAR = $("#hdfCFOPESCRITURAR").val();

    $("#txtCFOP_XML").attr('disabled', true);
    $("#txtCFOP_XML").val(pCFOP_XML);
    $("#txtCFOP_SAP").val(pCFOP_SAP);
    $("#txtCFOP_ESCRITURAR").val(PCFOP_ESCRITURAR);
    
    $("#idItem").val(pEditar);
}

function Excluir() {
    pCFOP_XML = $("#hdfCFOPXML").val();
    pCFOP_SAP = $("#hdfCFOPSAP").val();
    PCFOP_ESCRITURAR = $("#hdfCFOPESCRITURAR").val();

    $.SmartMessageBox({
        title: "Excluir Padrão",
        content: "Deseja realmente excluir o Padrão?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: false,
                cache: false,
                type: 'GET',
                url: url + "CFOP_MIRO/Excluir",
                dataType: 'json',
                data: {
                    pCFOP_XML : pCFOP_XML,
                    pCFOP_SAP : pCFOP_SAP,
                    PCFOP_ESCRITURAR: PCFOP_ESCRITURAR
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Exclusão efetuado com sucesso.",
                        color: "#739E73",
                        icon: "fa fa-trash-o fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                }
            });
            Atualizar();
        }
    });
    

}