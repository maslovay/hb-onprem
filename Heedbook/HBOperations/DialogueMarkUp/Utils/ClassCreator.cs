using System;
using HBData.Models;

namespace DialogueMarkUp.Utils
{
    public class ClassCreator
    {

        public Dialogue CreateDialogueClass(Guid dialogueId, Guid applicationUserId, DateTime begTime, DateTime endTime, string personFaceDescriptor)
        {
            return new CreateDialogue(dialogueId, applicationUserId, begTime, endTime, personFaceDescriptor).GetDialogue();
        }

        public DialogueMarkup CreateMarkUpClass(Guid applicationUserId, DateTime begTime, DateTime endTime)
        {
            return new CreateMarkUp(applicationUserId, begTime, endTime).GetMarkUp();
        }

        private class CreateDialogue
        {
            private Dialogue _dialogue;
            public CreateDialogue(Guid dialogueId, Guid applicationUserId, DateTime begTime, DateTime endTime, string personFaceDescriptor)
            {
                _dialogue = new Dialogue{
                    DialogueId = dialogueId,
                    ApplicationUserId = applicationUserId,
                    BegTime = begTime,
                    EndTime = endTime,
                    CreationTime = DateTime.UtcNow,
                    LanguageId = 1,
                    StatusId = 6,
                    InStatistic = true
                };
            }

            public Dialogue GetDialogue()
            {
                return _dialogue;
            }
        }

        private class CreateMarkUp
        {
            private HBData.Models.DialogueMarkup _markup;

            public CreateMarkUp(Guid applicationUserId, DateTime begTime, DateTime endTime)
            {
                _markup = new HBData.Models.DialogueMarkup{
                    DialogueMarkUpId = Guid.NewGuid(),
                    ApplicationUserId = applicationUserId,
                    BegTime = begTime,
                    BegTimeMarkup = begTime,
                    EndTime = endTime,
                    EndTimeMarkup = endTime,
                    IsDialogue = true,
                    CreationTime = DateTime.UtcNow,
                    StatusId = 7,
                    TeacherId = "NN"
                };
            }

            public HBData.Models.DialogueMarkup GetMarkUp()
            {
                return _markup;
            }
        }
    }
}