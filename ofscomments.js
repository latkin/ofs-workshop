var azureFuncUrl = 'https://lda-ofs-workshop.azurewebsites.net/api/ofs-comments';
var ofs_postId = null;

// entry point
function OFSCommentsInit(postId) {
    ofs_postId = postId;
    $("#ofs-comments").append("<div id='ofs-commentlist'/>");
    $("#ofs-comments").append("<div id='ofs-submitcomment'/>");

    LoadComments();
    LoadCommentSubmission();
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

// adds the comment submission text boxes
// and buttons into the page
function LoadCommentSubmission() {
    var t = "<form id='ofs-submit-form'>";
    t += "<fieldset>";
    t += "<legend>Leave a comment</legend>";
    t += "Name<br>";
    t += "<input type='text' id='ofs-submit-name'><br>";
    t += "Comment<br>";
    t += "<textarea id='ofs-submit-comment' cols='80' rows='10' /><br><br>";
    t += "<input type='submit' id='ofs-submit-postcomment' value='Post comment'>";
    t += "</fieldset>";
    t += "</form>";

    $("#ofs-submitcomment").append(t);

    $("#ofs-submit-postcomment").click(function (e) {
        e.preventDefault();
        SubmitAndRenderComment(ofs_postId, $("#ofs-submit-name").val(), $("#ofs-submit-comment").val());
    });
}

// helper to disable/enable the various text boxes and buttons
// used for submitting comments
function ToggleSubmissionControls(enabled) {
    $("#ofs-submit-name").prop("disabled", !enabled);
    $("#ofs-submit-comment").prop("disabled", !enabled);
    $("#ofs-submit-postcomment").prop("disabled", !enabled);
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

// submits a single comment to the server
function SubmitAndRenderComment(postId, name, comment) {
    ToggleSubmissionControls(false);

    $.ajax(azureFuncUrl, {
        data: JSON.stringify({ postid: postId, name: name, comment: comment }),
        type: "POST",
        contentType: "application/json",
        dataType: "json",
        success: function (data, status, xhr) {
            AddSingleComment(data);
            $("#ofs-submit-name").val("");
            $("#ofs-submit-comment").val("");
            ToggleSubmissionControls(true);
        },
        error: function (err) {
            var data = $.parseJSON(err.responseText);
            if (data.Message) {
                $("#ofs-commentlist").append("<div class='ofs-error'>Error adding comment: " + data.Message + "</div>");
            } else {
                $("#ofs-commentlist").append("<div class='ofs-error'>Error adding comment</div>");
            }
            ToggleSubmissionControls(true);
        }
    });
}