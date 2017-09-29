var azureFuncUrl = 'https://lda-ofs-workshop.azurewebsites.net/api/ofs-comments';
var ofs_postId = null;

// entry point
function OFSCommentsInit(postId) {
    ofs_postId = postId;
    $("#ofs-comments").append("<div id='ofs-commentlist'/>");

    LoadComments();
}

// pulls down array of comments for this post and inserts them
function LoadComments() {
    var url = azureFuncUrl + "?postid=" + ofs_postId;
    $.ajax(url, {
        dataType: "json",
        success: function (comments) {
            $.each(comments, function (i, comment) {
                AddSingleComment(comment);
            });
        },
        error: function () {
            $("#ofs-commentlist").append("<div class='ofs-error'>Error retrieving comments.</div>");
        }
    });
}

// inserts a single comment
function AddSingleComment(comment) {
    var date = new Date(comment.time);
    var t = "<div class='ofs-comment'>";
    t += "<div class='ofs-commentheader'><b>" + comment.name + "</b>";
    t += " posted at ";
    t += "<em>" + date.toLocaleString() + "</em></div>";
    t += "<div class='ofs-commentbody'>";
    t += comment.comment;
    t += "</div>";
    $("#ofs-commentlist").append(t);
}