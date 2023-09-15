;; invoke: add (i32:2) (i32:3)
;; expect: (i32:5)

(module
    (func (export "add") (param $x i32) (param $y i32) (result i32)
        local.get $x
        local.get $y
        i32.add
    )
)