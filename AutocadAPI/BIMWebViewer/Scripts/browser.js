var viewer = null;
var browser = null;
$(document).ready(function () {
    //declare viewer and browser at the beginning so that it can be used as a variable before it is initialized.


    function initControls() {

        $("#semantic-descriptive-info").accordion({
            heightStyle: "fill"
        });
        $("#semantic-model").accordion({
            heightStyle: "fill"
        });

        $("#btnLocate").button().click(function () {
            var id = $(this).data("id");
            if (typeof (id) != "undefined" && viewer) {
                viewer.zoomTo(parseInt(id));
            }
        });

        $("#toolbar button").button();

        $("#btnClip").click(function () {
            debugger;
            viewer.clip();
        });

        $("#btnUnclip").click(function () {
            viewer.unclip();
        });

    }
    function reinitControls() {
        $("#semantic-model").accordion("refresh");
        $("#semantic-descriptive-info").accordion("refresh");
    }
    initControls();
    $(window).resize(function () {
        reinitControls();
    });

    var keepTarget = false;
    browser = new xBrowser();
    browser.on("loaded", function (args) {
        var facility = args.model.facility;
        //render parts
        browser.renderSpatialStructure("structure", true);
        browser.renderAssetTypes("assetTypes", true);
        browser.renderSystems("systems");
        browser.renderZones("zones");
        browser.renderContacts("contacts");
        browser.renderDocuments(facility[0], "facility-documents");

        //open and selectfacility node
        $("#structure > ul > li").click();
    });

    browser.on("entityClick", function (args) {
        var span = $(args.element).children("span.ACY-entity");
        if (document._lastSelection)
            document._lastSelection.removeClass("ui-selected");
        span.addClass("ui-selected")
        document._lastSelection = span;
    });
    browser.on("entityActive", function (args) {
        var isRightPanelClick = false;
        if (args.element)
            if ($(args.element).parents("#semantic-descriptive-info").length != 0)
                isRightPanelClick = true;

        //set ID for location button
        $("#btnLocate").data("id", args.entity.id);

        browser.renderPropertiesAttributes(args.entity, "attrprop");
        browser.renderAssignments(args.entity, "assignments");
        browser.renderDocuments(args.entity, "documents");
        browser.renderIssues(args.entity, "issues");

        if (isRightPanelClick)
            $("#attrprop-header").click();

    });

    browser.on("entityDblclick", function (args) {
        var entity = args.entity;
        var allowedTypes = ["space", "assettype", "asset"];
        if (allowedTypes.indexOf(entity.type) === -1) return;

        var id = parseInt(entity.id);
        if (id && viewer) {
            viewer.resetStates();
            viewer.renderingMode = "x-ray";
            if (entity.type === "assettype") {
                var ids = [];
                for (var i = 0; i < entity.children.length; i++) {
                    id = parseInt(entity.children[i].id);
                    ids.push(id);
                }
                viewer.setState(xState.HIGHLIGHTED, ids);
            }
            else {
                viewer.setState(xState.HIGHLIGHTED, [id]);
            }
            viewer.zoomTo(id);
            keepTarget = true;
        }
    });


     
});