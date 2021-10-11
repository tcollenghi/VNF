/// <reference path="../../libs/jquery-2.1.1.min.js" />

function selectRow(id, desc) {
    $("#hdfId").val(id);
    $("#hdfDesc").val(desc);
}

function Salvar() {
    id = $("#hdfId").val();
    descricao = $("#txtDescricao").val();

    var erros = "";
        
    if (descricao == "") {
        erros += "<p>Informar a descrição</p>"
    }

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#IdEdit").val();
    if (Editar == 'EDITAR') {
        UrlParametro =  url + "StatusMaterialBloqueados/Editar";
        pData = {
            id_status_material_bloqueado: id,
            smb_descricao: descricao
        }
    }
    else {
        UrlParametro = url + "StatusMaterialBloqueados/Salvar";
        pData = {
            smb_descricao: descricao
        }
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar o Status Material Bloqueado",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {            
            $("#txtDescricao").val('');

            $.smallBox({
                title: "Alerta",
                content: "Status Material Bloqueado realizada com sucesso",
                timeout: 4000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
        }
    });

}

function Editar(pEditar) {
    $("#idTitulo").text("Editar");
    id = $("#hdfId").val();
    descricao = $("#hdfDesc").val();   
    $("#txtDescricao").val(descricao);
    $("#IdEdit").val(pEditar);
}


function Novo() {
    $("#idTitulo").text("Novo");
    $("#txtDescricao").val('');
    $("#hdfId").val('');
    $("#hdfDesc").val('');
    $(".selecionado").removeClass("selecionado");
}