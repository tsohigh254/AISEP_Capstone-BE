// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/// @title  DocumentRegistry — AISEP Document Hash Notarization
/// @notice Lưu SHA-256 hash tài liệu lên Ethereum để chứng minh tính toàn vẹn.
/// @dev    Mỗi hash chỉ được đăng ký 1 lần (immutable). Metadata lưu dạng JSON string
///         để backend tự quyết nội dung mà không cần thay đổi contract.
contract DocumentRegistry {

    // ═══════════════════════════════════════════════════════════
    //  State
    // ═══════════════════════════════════════════════════════════

    address public owner;
    address public pendingOwner; // 2-step ownership transfer

    struct Document {
        bytes32 fileHash;
        address submitter;
        uint256 timestamp;      // block.timestamp khi đăng ký — luôn > 0
        string  metadata;       // JSON: {"documentId":1,"startupId":5,...}
    }

    mapping(bytes32 => Document) private _documents;
    mapping(address => bytes32[]) private _submitterHashes;
    mapping(address => bool) public authorizedSubmitters;

    // ═══════════════════════════════════════════════════════════
    //  Events
    // ═══════════════════════════════════════════════════════════

    event DocumentRegistered(
        bytes32 indexed fileHash,
        address indexed submitter,
        uint256 timestamp,
        string  metadata
    );

    event OwnershipTransferStarted(address indexed previousOwner, address indexed newOwner);
    event OwnershipTransferred(address indexed previousOwner, address indexed newOwner);
    event SubmitterAuthorized(address indexed account);
    event SubmitterRevoked(address indexed account);

    // ═══════════════════════════════════════════════════════════
    //  Errors (custom errors tiết kiệm gas hơn require string)
    // ═══════════════════════════════════════════════════════════

    error NotOwner();
    error NotAuthorized();
    error ZeroHash();
    error HashAlreadyRegistered();
    error DocumentNotFound();
    error ZeroAddress();
    error NotPendingOwner();

    // ═══════════════════════════════════════════════════════════
    //  Modifiers
    // ═══════════════════════════════════════════════════════════

    modifier onlyOwner() {
        if (msg.sender != owner) revert NotOwner();
        _;
    }

    modifier onlyAuthorized() {
        if (msg.sender != owner && !authorizedSubmitters[msg.sender])
            revert NotAuthorized();
        _;
    }

    // ═══════════════════════════════════════════════════════════
    //  Constructor
    // ═══════════════════════════════════════════════════════════

    constructor() {
        owner = msg.sender;
        authorizedSubmitters[msg.sender] = true;
        emit OwnershipTransferred(address(0), msg.sender);
        emit SubmitterAuthorized(msg.sender);
    }

    // ═══════════════════════════════════════════════════════════
    //  Ownership (2-step: initiate → accept)
    // ═══════════════════════════════════════════════════════════

    /// @notice Bước 1: Owner đề cử owner mới
    function transferOwnership(address _newOwner) external onlyOwner {
        if (_newOwner == address(0)) revert ZeroAddress();
        pendingOwner = _newOwner;
        emit OwnershipTransferStarted(owner, _newOwner);
    }

    /// @notice Bước 2: Owner mới xác nhận (tránh chuyển nhầm sang ví sai)
    function acceptOwnership() external {
        if (msg.sender != pendingOwner) revert NotPendingOwner();
        emit OwnershipTransferred(owner, msg.sender);
        owner = msg.sender;
        pendingOwner = address(0);
        // Owner mới tự động được authorize
        authorizedSubmitters[msg.sender] = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  Access Control
    // ═══════════════════════════════════════════════════════════

    /// @notice Thêm ví được phép đăng ký hash (ví dụ: backend service wallet)
    function authorizeSubmitter(address _account) external onlyOwner {
        if (_account == address(0)) revert ZeroAddress();
        authorizedSubmitters[_account] = true;
        emit SubmitterAuthorized(_account);
    }

    /// @notice Thu hồi quyền đăng ký
    function revokeSubmitter(address _account) external onlyOwner {
        authorizedSubmitters[_account] = false;
        emit SubmitterRevoked(_account);
    }

    // ═══════════════════════════════════════════════════════════
    //  Core: Register & Verify
    // ═══════════════════════════════════════════════════════════

    /// @notice Đăng ký hash tài liệu. Mỗi hash chỉ được đăng ký 1 lần.
    /// @param  _fileHash  SHA-256 hash (32 bytes)
    /// @param  _metadata  JSON string chứa thông tin bổ sung (tùy backend)
    function registerDocument(
        bytes32 _fileHash,
        string calldata _metadata
    ) external onlyAuthorized {
        if (_fileHash == bytes32(0)) revert ZeroHash();
        if (_documents[_fileHash].timestamp != 0) revert HashAlreadyRegistered();

        _documents[_fileHash] = Document({
            fileHash:  _fileHash,
            submitter: msg.sender,
            timestamp: block.timestamp,
            metadata:  _metadata
        });

        _submitterHashes[msg.sender].push(_fileHash);

        emit DocumentRegistered(_fileHash, msg.sender, block.timestamp, _metadata);
    }

    /// @notice Kiểm tra hash có tồn tại trên chain không — trả bool đơn giản
    /// @dev    Map trực tiếp với VerifyHashAsync(fileHash) → bool trong C# backend
    function existsDocument(bytes32 _fileHash) external view returns (bool) {
        return _documents[_fileHash].timestamp != 0;
    }

    /// @notice Kiểm tra hash + trả thêm thông tin submitter và timestamp
    function verifyDocument(bytes32 _fileHash)
        external view
        returns (bool exists, address submitter, uint256 timestamp)
    {
        Document storage doc = _documents[_fileHash];
        return (doc.timestamp != 0, doc.submitter, doc.timestamp);
    }

    /// @notice Lấy toàn bộ thông tin document (cho staff/admin verify chi tiết)
    function getDocument(bytes32 _fileHash)
        external view
        returns (Document memory)
    {
        if (_documents[_fileHash].timestamp == 0) revert DocumentNotFound();
        return _documents[_fileHash];
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    /// @notice Số document mà 1 address đã đăng ký
    function getSubmitterDocumentCount(address _submitter)
        external view
        returns (uint256)
    {
        return _submitterHashes[_submitter].length;
    }

    /// @notice Lấy hash theo index của 1 submitter (phục vụ pagination)
    function getSubmitterDocumentAt(address _submitter, uint256 _index)
        external view
        returns (bytes32)
    {
        return _submitterHashes[_submitter][_index];
    }
}
