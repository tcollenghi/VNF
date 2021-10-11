$(document).ready(function () {
    pageSetUp();
    setMaskCnpj("CNPJ");
})

function goBack(){
    window.history.back();
    $("#hdfId").val('');
    $("#hdfRE").val('');
}

function abrirDetalheNota(id) {
    window.location = url + 'compras/edit/' + id;
}