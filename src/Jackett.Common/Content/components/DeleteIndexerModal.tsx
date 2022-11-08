import { Modal, Text } from "@geist-ui/core";
import { ModalHooksBindings } from "@geist-ui/core/esm/use-modal";
import { Dispatch, SetStateAction } from "react";

type ModalProps = {
	bindings: ModalHooksBindings;
	setVisible: Dispatch<SetStateAction<boolean>>;
	remove: () => void;
};

export default function DeleteIndexerModal({
	bindings,
	setVisible,
	remove,
}: ModalProps) {
	return (
		<Modal {...bindings}>
			<Modal.Title>Delete Indexer</Modal.Title>
			<Modal.Content>
				<Text>Are you sure you want to delete this indexer?</Text>
			</Modal.Content>
			<Modal.Action passive onClick={() => setVisible(false)}>
				Cancel
			</Modal.Action>
			<Modal.Action>
				<Text type="error" onClick={remove}>
					Delete
				</Text>
			</Modal.Action>
		</Modal>
	);
}
