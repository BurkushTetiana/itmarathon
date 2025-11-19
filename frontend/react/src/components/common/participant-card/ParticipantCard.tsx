import CopyButton from "../copy-button/CopyButton";
import InfoButton from "../info-button/InfoButton";
import ItemCard from "../item-card/ItemCard";
import { type ParticipantCardProps } from "./types";
import "./ParticipantCard.scss";

const ParticipantCard = ({
    firstName,
    lastName,
    isCurrentUser = false,
    isAdmin = false,
    isCurrentUserAdmin = false,
    adminInfo = "",
    participantLink = "",
    onInfoButtonClick,
}: ParticipantCardProps) => {

    const handleDelete = () => {
        if (window.confirm(`Delete ${firstName} ${lastName} from room?`)) {
            console.log("Deleting user:", firstName, lastName);
            // API call will be here
        }
    };

    return (
        <ItemCard title={`${firstName} ${lastName}`} isFocusable>
            <div className="participant-card-info-container">
                {isCurrentUser ? <p className="participant-card-role">You</p> : null}

                {!isCurrentUser && isAdmin ? (
                    <p className="participant-card-role">Admin</p>
                ) : null}

                {isCurrentUserAdmin ? (
                    <CopyButton
                        textToCopy={participantLink}
                        iconName="link"
                        successMessage="Personal Link is copied!"
                        errorMessage="Personal Link was not copied. Try again."
                    />
                ) : null}

                {isCurrentUserAdmin && !isAdmin ? (
                    <InfoButton withoutToaster onClick={onInfoButtonClick} />
                ) : null}

                {!isCurrentUser && isAdmin ? (
                    <InfoButton infoMessage={adminInfo} />
                ) : null}

                {/* DELETE BUTTON - EXACTLY LIKE IN FIGMA */}
                {isCurrentUserAdmin && !isCurrentUser && !isAdmin ? (
                    <button
                        className="delete-button"
                        onClick={handleDelete}
                        style={{
                            display: 'flex',
                            flexDirection: 'row',
                            justifyContent: 'center',
                            alignItems: 'center',
                            padding: '0px',
                            gap: '8px',
                            width: '40px',
                            height: '40px',
                            background: 'none',
                            border: 'none',
                            cursor: 'pointer'
                        }}
                        title="Delete user"
                    >
                        {/* Delete Icon */}
                        <svg
                            width="24"
                            height="24"
                            viewBox="0 0 24 24"
                            fill="none"
                            style={{
                                flex: 'none',
                                order: 0,
                                flexGrow: 0
                            }}
                        >
                            <path
                                d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"
                                fill="#517459" // Green500 from Figma
                            />
                        </svg>
                    </button>
                ) : null}
            </div>
        </ItemCard>
    );
};

export default ParticipantCard;